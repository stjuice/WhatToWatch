using ImdbWatchlists.Models;

namespace ImdbWatchlists;

public interface IImdbWatchlists
{
    Task<Watchlist> GetListAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default);
}
