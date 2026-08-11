using ImdbWatchlists.Models;
using ImdbWatchlists.Providers;
using ImdbWatchlists.Services;

namespace ImdbWatchlists.Tests.Services;

public class WatchlistServiceTests
{
    [Fact]
    public async Task GetListAsync_UsesPublicProvider_WhenAccessIsPublic()
    {
        var publicProvider = new FakeWatchlistProvider(WatchlistAccess.Public);
        var privateProvider = new FakeWatchlistProvider(WatchlistAccess.Private);
        var service = new WatchlistService([publicProvider, privateProvider]);

        await service.GetListAsync(
            new WatchlistRequest
            {
                Url = "https://www.imdb.com/list/ls000000000/",
                Access = WatchlistAccess.Public
            });

        Assert.Equal(1, publicProvider.CallCount);
        Assert.Equal(0, privateProvider.CallCount);
    }

    [Fact]
    public async Task GetListAsync_UsesPrivateProvider_WhenAccessIsPrivate()
    {
        var publicProvider = new FakeWatchlistProvider(WatchlistAccess.Public);
        var privateProvider = new FakeWatchlistProvider(WatchlistAccess.Private);
        var service = new WatchlistService([publicProvider, privateProvider]);

        await service.GetListAsync(
            new WatchlistRequest
            {
                Url = "https://www.imdb.com/user/ur00000000/watchlist",
                Access = WatchlistAccess.Private
            });

        Assert.Equal(0, publicProvider.CallCount);
        Assert.Equal(1, privateProvider.CallCount);
    }

    private sealed class FakeWatchlistProvider : IWatchlistProvider
    {
        public FakeWatchlistProvider(WatchlistAccess access)
        {
            Access = access;
        }

        public WatchlistAccess Access { get; }

        public int CallCount { get; private set; }

        public Task<Watchlist> GetWatchlistAsync(
            WatchlistRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new Watchlist
            {
                Id = "test",
                Name = "Test",
                Url = request.Url,
                Movies = []
            });
        }
    }
}
