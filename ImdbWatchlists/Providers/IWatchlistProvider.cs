using ImdbWatchlists.Models;

namespace ImdbWatchlists.Providers;

public interface IWatchlistProvider
{
    WatchlistAccess Access { get; }

    Task<Watchlist> GetWatchlistAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default);
}
