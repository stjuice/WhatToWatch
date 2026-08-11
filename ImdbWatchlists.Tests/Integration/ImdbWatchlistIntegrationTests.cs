using ImdbWatchlists.Models;
using ImdbWatchlists.Providers;
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
            await fixture.GetBrowserManagerAsync());

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
}
