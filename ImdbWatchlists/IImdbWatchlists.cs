using ImdbWatchlists.Models;

namespace ImdbWatchlists;

/// <summary>
/// Fetches watchlists from IMDb. MVP uses public lists; private access is reserved for a future provider.
/// </summary>
public interface IImdbWatchlists
{
    Task<Watchlist> GetListAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default);
}
