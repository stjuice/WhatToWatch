using WhatToWatch.Models;

namespace WhatToWatch.Services;

public interface IWatchlistService
{
    Task<Watchlist?> GetWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Watchlist>> GetWatchlistsAsync(
        CancellationToken cancellationToken = default);

    Task RefreshWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default);
}
