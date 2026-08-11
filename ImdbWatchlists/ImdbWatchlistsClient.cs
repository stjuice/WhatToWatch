using ImdbWatchlists.Models;
using ImdbWatchlists.Services;

namespace ImdbWatchlists;

public sealed class ImdbWatchlistsClient(WatchlistService watchlistService) : IImdbWatchlists
{
    public Task<Watchlist> GetListAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        return watchlistService.GetListAsync(request, cancellationToken);
    }
}
