using WhatToWatch.DTOs;
using WhatToWatch.Models;

namespace WhatToWatch.Services;

public interface IWatchlistService
{
    Task<Watchlist> ImportAsync(
        string url,
        CancellationToken cancellationToken = default);

    Task<Watchlist> ImportFromImdbPayloadAsync(
        ImportImdbWatchlistRequest request,
        CancellationToken cancellationToken = default);

    Task<Watchlist?> GetWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Watchlist>> GetWatchlistsAsync(
        CancellationToken cancellationToken = default);

    Task<Watchlist?> UpdateWatchlistAsync(
        string id,
        UpdateWatchlistRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<Watchlist?> RefreshWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default);
}
