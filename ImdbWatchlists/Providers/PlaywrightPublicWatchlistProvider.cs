using ImdbWatchlists.Browser;
using ImdbWatchlists.Models;
using ImdbWatchlists.Options;
using ImdbWatchlists.Parsing;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImdbWatchlists.Providers;

public sealed class PlaywrightPublicWatchlistProvider(
    IBrowserManager browserManager,
    IOptions<ImdbWatchlistsOptions> options)
    : IWatchlistProvider
{
    private const int MaxPages = 40;
    private const float ListDataTimeoutMs = 30_000;

    private const int HumanVerificationStatus = 405;

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
        var response = await page.GotoAsync(url, new PageGotoOptions
        {
            Timeout = 60_000,
            WaitUntil = WaitUntilState.DOMContentLoaded,
        }).WaitAsync(cancellationToken);

        if (response is null || (response.Status >= 400 && response.Status != HumanVerificationStatus))
            throw new ImdbWatchlistException(
                $"IMDb returned HTTP {response?.Status.ToString() ?? "unknown"} for '{url}'.");

        await WaitForListDataAsync(page, url, response.Status, cancellationToken);

        return await page.ContentAsync().WaitAsync(cancellationToken);
    }

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
