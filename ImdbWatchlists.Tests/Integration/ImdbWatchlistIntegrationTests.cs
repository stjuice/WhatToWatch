using ImdbWatchlists.Extraction;
using ImdbWatchlists.Models;
using ImdbWatchlists.Options;
using ImdbWatchlists.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace ImdbWatchlists.Tests.Integration;

public class ImdbWatchlistIntegrationTests(ImdbBrowserFixture fixture, ITestOutputHelper output)
    : IClassFixture<ImdbBrowserFixture>
{
    private const string SkipReason =
        "Hits imdb.com with a real browser. Remove Skip to run manually.";

    private const string WatchlistUrl =
        "https://www.imdb.com/user/p.sx47zylgs4uarc76oeqpyxoheq/watchlist/?ref_=ext_shr_lnk";

    [Fact(Skip = SkipReason)]
    public async Task GetWatchlistAsync_ReadsMoviesFromRealWatchlist()
    {
        var provider = new PlaywrightPublicWatchlistProvider(
            await fixture.GetBrowserManagerAsync(),
            new PlaywrightJsImdbPageExtractor(),
            Microsoft.Extensions.Options.Options.Create(new ImdbWatchlistsOptions()),
            NullLogger<PlaywrightPublicWatchlistProvider>.Instance);

        var watchlist = await provider.GetWatchlistAsync(
            new WatchlistRequest { Url = WatchlistUrl });

        output.WriteLine($"Name: {watchlist.Name}");
        output.WriteLine($"Movies: {watchlist.Movies.Count}");
        foreach (var movie in watchlist.Movies.Take(10))
        {
            output.WriteLine(
                $"  {movie.Id} {movie.Title} ({movie.Year?.ToString() ?? "-"}) " +
                $"rating={movie.Rating?.ToString() ?? "-"} genres={string.Join('/', movie.Genres)}");
        }

        Assert.Equal("p.sx47zylgs4uarc76oeqpyxoheq", watchlist.Id);
        Assert.True(watchlist.Movies.Count > 250, "Expected pagination past the first page.");
        Assert.All(watchlist.Movies, movie =>
        {
            Assert.StartsWith("tt", movie.Id);
            Assert.False(string.IsNullOrWhiteSpace(movie.Title));
        });
    }

    /// <summary>
    /// Live smoke: loads only page 1 of a public IMDb list and checks parsing,
    /// including optional Plot / RuntimeMinutes / Director when IMDb embeds them.
    /// </summary>
    [Fact(Skip = SkipReason)]
    public async Task ParsePage_ReadsMoviesAndOptionalDetailFields_FromLiveImdb()
    {
        var browser = await fixture.GetBrowserManagerAsync();
        var context = await browser.GetContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            const string listUrl = "https://www.imdb.com/list/ls055592025/";
            var response = await page.GotoAsync(listUrl, new PageGotoOptions
            {
                Timeout = 60_000,
                WaitUntil = WaitUntilState.DOMContentLoaded,
            });

            Assert.NotNull(response);
            Assert.True(response.Ok, $"IMDb returned HTTP {response.Status}");

            await page.WaitForSelectorAsync(
                "script#__NEXT_DATA__",
                new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 30_000,
                });

            var extractor = new PlaywrightJsImdbPageExtractor();
            var extracted = await extractor.ExtractCurrentPageAsync(page);
            var listPage = ImdbExtractedPageMapper.ToListPage(extracted, listUrl, listUrl);
            var watchlist = listPage.Watchlist;

            output.WriteLine($"Name: {watchlist.Name}");
            output.WriteLine($"Movies on page 1: {watchlist.Movies.Count}");
            output.WriteLine($"HasNextPage: {listPage.HasNextPage}");

            Assert.False(string.IsNullOrWhiteSpace(watchlist.Name));
            Assert.NotEmpty(watchlist.Movies);

            var sample = watchlist.Movies.Take(15).ToList();
            var withPlot = sample.Count(movie => !string.IsNullOrWhiteSpace(movie.Plot));
            var withRuntime = sample.Count(movie => movie.RuntimeMinutes is > 0);
            var withDirector = sample.Count(movie => !string.IsNullOrWhiteSpace(movie.Director));

            foreach (var movie in sample)
            {
                var plotPreview = string.IsNullOrWhiteSpace(movie.Plot)
                    ? "-"
                    : movie.Plot[..Math.Min(60, movie.Plot.Length)] + "...";

                output.WriteLine(
                    $"  {movie.Id} {movie.Title} ({movie.Year?.ToString() ?? "-"}) " +
                    $"rating={movie.Rating?.ToString() ?? "-"} " +
                    $"runtime={movie.RuntimeMinutes?.ToString() ?? "-"} " +
                    $"director={movie.Director ?? "-"} " +
                    $"plot={plotPreview}");
            }

            output.WriteLine(
                $"Detail field coverage (first {sample.Count}): " +
                $"plot={withPlot}, runtime={withRuntime}, director={withDirector}");

            Assert.All(watchlist.Movies, movie =>
            {
                Assert.StartsWith("tt", movie.Id);
                Assert.False(string.IsNullOrWhiteSpace(movie.Title));
            });
            Assert.True(withPlot > 0, "Expected at least one plot from the live list embed.");
            Assert.True(withRuntime > 0, "Expected at least one runtime from the live list embed.");
            Assert.True(withDirector > 0, "Expected at least one director from the live list embed.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
