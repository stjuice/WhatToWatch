using System.Text.Json;
using ImdbWatchlists.Browser;
using ImdbWatchlists.Models;
using ImdbWatchlists.Options;
using ImdbWatchlists.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImdbWatchlists.Providers;

public sealed class PlaywrightPublicWatchlistProvider(
    IBrowserManager browserManager,
    IOptions<ImdbWatchlistsOptions> options,
    ILogger<PlaywrightPublicWatchlistProvider> logger)
    : IWatchlistProvider
{
    private const int MaxPages = 40;
    private const int BodyPreviewLength = 300;
    private const float ListDataTimeoutMs = 30_000;

    private readonly ImdbWatchlistsOptions _options = options.Value;

    public WatchlistAccess Access => WatchlistAccess.Public;

    public async Task<Watchlist> GetWatchlistAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listUrl = ImdbListUrl.Normalize(request.Url);

        var context = await browserManager.GetContextAsync(cancellationToken);
        var page = await context.NewPageAsync().WaitAsync(cancellationToken);

        try
        {
            var userAgent = await page.EvaluateAsync<string>("() => navigator.userAgent")
                .WaitAsync(cancellationToken);
            logger.LogInformation(
                "IMDb Playwright browser diagnostics: UserAgent={UserAgent}; " +
                "UsesPersistentContext={UsesPersistentContext}; StorageStateLoaded={StorageStateLoaded}",
                userAgent,
                browserManager.UsesPersistentContext,
                browserManager.StorageStateLoaded);

            Watchlist? watchlist = null;
            var movies = new List<Movie>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (var pageNumber = 1; pageNumber <= MaxPages; pageNumber++)
            {
                var pageUrl = ImdbListUrl.WithPage(listUrl, pageNumber);
                var html = await LoadAsync(page, pageUrl, cancellationToken);
                var listPage = ImdbListHtmlParser.ParsePage(html, listUrl);

                watchlist ??= listPage.Watchlist;

                foreach (var movie in listPage.Watchlist.Movies)
                {
                    if (seenIds.Add(movie.Id))
                        movies.Add(movie);
                }

                if (!listPage.HasNextPage)
                    break;
            }

            await browserManager.PersistStorageStateAsync(cancellationToken);
            return watchlist! with { Movies = movies };
        }
        catch (PlaywrightException exception)
        {
            throw new ImdbWatchlistException(
                $"Playwright could not load IMDb watchlist '{request.Url}'.", exception);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<string> LoadAsync(
        IPage page,
        string url,
        CancellationToken cancellationToken)
    {
        IResponse? response = null;
        string html = string.Empty;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            response = await page.GotoAsync(url, new PageGotoOptions
            {
                Timeout = 60_000,
                WaitUntil = WaitUntilState.DOMContentLoaded,
            }).WaitAsync(cancellationToken);

            if (response is null)
                throw new ImdbWatchlistException(
                    $"IMDb navigation to '{page.Url ?? url}' returned no HTTP response.");

            html = await page.ContentAsync().WaitAsync(cancellationToken);
            if (response.Ok)
                break;

            if (response.Status == 403 && attempt < 3)
            {
                logger.LogWarning(
                    "IMDb returned HTTP 403 for {Url} (attempt {Attempt}/3). Retrying.",
                    url,
                    attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            var finalUrl = string.IsNullOrWhiteSpace(response.Url)
                ? page.Url ?? url
                : response.Url;
            var preview = GetBodyPreview(html);
            var headers = await response.AllHeadersAsync().WaitAsync(cancellationToken);

            logger.LogWarning(
                "IMDb navigation returned a non-success response. FinalUrl={FinalUrl}; " +
                "StatusCode={StatusCode}; ResponseHeaders={ResponseHeaders}; " +
                "HtmlPreview={HtmlPreview}; UsesPersistentContext={UsesPersistentContext}; " +
                "StorageStateLoaded={StorageStateLoaded}",
                finalUrl,
                response.Status,
                JsonSerializer.Serialize(headers),
                preview,
                browserManager.UsesPersistentContext,
                browserManager.StorageStateLoaded);

            throw new ImdbWatchlistException(FormatHttpError(response.Status, finalUrl, preview));
        }

        await WaitForListDataAsync(page, url, response!.Status, cancellationToken);

        return html;
    }

    private static string FormatHttpError(int status, string url, string preview)
    {
        if (status == 403)
        {
            return
                "IMDb blocked this server from opening the list (HTTP 403). " +
                "Render datacenter IPs are often blocked unless a saved IMDb session " +
                "is on the disk at /var/data/imdb-session.json " +
                "(run ImdbSessionBootstrap locally, copy the file, restart the service). " +
                "Android import still works because it uses your phone's browser. " +
                $"URL: {url}";
        }

        return $"IMDb returned HTTP {status} for '{url}'. Body preview: {preview}";
    }

    private static string GetBodyPreview(string html) =>
        html[..Math.Min(html.Length, BodyPreviewLength)];

    private async Task WaitForListDataAsync(
        IPage page,
        string url,
        int status,
        CancellationToken cancellationToken)
    {
        if (await TryWaitForListDataAsync(page, ListDataTimeoutMs, cancellationToken))
            return;

        var manualWindow = _options.BrowserHeadless
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(_options.ManualChallengeTimeoutSeconds);

        if (manualWindow > TimeSpan.Zero
            && await TryWaitForListDataAsync(
                page, (float)manualWindow.TotalMilliseconds, cancellationToken))
            return;

        throw new ImdbWatchlistException(
            $"IMDb served its human verification page for '{url}' (HTTP {status}). " +
            "Solve the challenge once with the ImdbSessionBootstrap tool to create a session " +
            "file, or clear it in a headed browser, then retry.");
    }

    private static async Task<bool> TryWaitForListDataAsync(
        IPage page,
        float timeoutMs,
        CancellationToken cancellationToken)
    {
        try
        {
            await page.WaitForSelectorAsync("script#__NEXT_DATA__", new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = timeoutMs,
            }).WaitAsync(cancellationToken);

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
