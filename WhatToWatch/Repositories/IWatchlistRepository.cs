using WhatToWatch.Models;

namespace WhatToWatch.Repositories;

public interface IWatchlistRepository
{
    Task<Watchlist?> GetAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<Watchlist?> GetByUrlAsync(
        string url,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Watchlist>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Watchlist watchlist,
        CancellationToken cancellationToken = default);
}
