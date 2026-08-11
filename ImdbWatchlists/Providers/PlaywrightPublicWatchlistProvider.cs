using ImdbWatchlists.Browser;
using ImdbWatchlists.Models;
using ImdbWatchlists.Parsing;
using Microsoft.Playwright;

namespace ImdbWatchlists.Providers;

public sealed class PlaywrightPublicWatchlistProvider(IBrowserManager browserManager)
    : IWatchlistProvider
{
    private const int MaxPages = 40;

    public WatchlistAccess Access => WatchlistAccess.Public;

    public async Task<Watchlist> GetWatchlistAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ImdbListUrl.ExtractId(request.Url);

        var context = await browserManager.GetContextAsync(cancellationToken);
        var page = await context.NewPageAsync().WaitAsync(cancellationToken);

        try
        {
            Watchlist? watchlist = null;
            var movies = new List<Movie>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (var pageNumber = 1; pageNumber <= MaxPages; pageNumber++)
            {
                var pageUrl = ImdbListUrl.WithPage(request.Url, pageNumber);
                var html = await LoadAsync(page, pageUrl, cancellationToken);
                var listPage = ImdbListHtmlParser.ParsePage(html, request.Url);

                watchlist ??= listPage.Watchlist;

                foreach (var movie in listPage.Watchlist.Movies)
                {
                    if (seenIds.Add(movie.Id))
                    {
                        movies.Add(movie);
                    }
                }

                if (!listPage.HasNextPage)
                {
                    break;
                }
            }

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

    private static async Task<string> LoadAsync(
        IPage page,
        string url,
        CancellationToken cancellationToken)
    {
        var response = await page.GotoAsync(url, new PageGotoOptions
        {
            Timeout = 60_000,
            WaitUntil = WaitUntilState.DOMContentLoaded,
        }).WaitAsync(cancellationToken);

        if (response is null || response.Status >= 400)
        {
            throw new ImdbWatchlistException(
                $"IMDb returned HTTP {response?.Status.ToString() ?? "unknown"} for '{url}'.");
        }

        await page.WaitForSelectorAsync("script#__NEXT_DATA__", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 30_000,
        }).WaitAsync(cancellationToken);

        return await page.ContentAsync().WaitAsync(cancellationToken);
    }
}
