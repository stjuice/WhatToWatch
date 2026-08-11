using ImdbWatchlists;
using ImdbWatchlists.Repositories;
using WhatToWatch.Mapping;
using WhatToWatch.Models;

namespace WhatToWatch.Services;

public class WatchlistService(
    IImdbWatchlists imdbWatchlists,
    IWatchlistRepository repository) : IWatchlistService
{
    public Task<Watchlist?> GetWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Watchlist>> GetWatchlistsAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RefreshWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
