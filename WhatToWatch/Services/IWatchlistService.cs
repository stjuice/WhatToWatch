using WhatToWatch.Models;

namespace WhatToWatch.Services;

public interface IWatchlistService
{
    Task<Watchlist> ImportAsync(
        string url,
        CancellationToken cancellationToken = default);

    Task<Watchlist?> GetWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Watchlist>> GetWatchlistsAsync(
        CancellationToken cancellationToken = default);

    Task<Watchlist?> RefreshWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default);
}
