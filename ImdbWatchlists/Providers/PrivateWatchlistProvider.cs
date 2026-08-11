using ImdbWatchlists.Models;

namespace ImdbWatchlists.Providers;

public class PrivateWatchlistProvider : IWatchlistProvider
{
    public WatchlistAccess Access => WatchlistAccess.Private;

    public Task<Watchlist> GetWatchlistAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Private IMDb watchlists are not supported in the MVP. Use WatchlistAccess.Public.");
    }
}
