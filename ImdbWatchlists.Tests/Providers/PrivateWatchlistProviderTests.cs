using ImdbWatchlists.Models;
using ImdbWatchlists.Providers;

namespace ImdbWatchlists.Tests.Providers;

public class PrivateWatchlistProviderTests
{
    [Fact]
    public async Task GetWatchlistAsync_ThrowsNotSupported_InMvp()
    {
        var provider = new PrivateWatchlistProvider();

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            provider.GetWatchlistAsync(
                new WatchlistRequest
                {
                    Url = "https://www.imdb.com/user/ur00000000/watchlist",
                    Access = WatchlistAccess.Private
                }));
    }
}
