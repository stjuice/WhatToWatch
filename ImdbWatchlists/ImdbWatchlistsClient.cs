using ImdbWatchlists.Models;
using ImdbWatchlists.Services;

namespace ImdbWatchlists;

/// <summary>
/// Public entry point for the library. WhatToWatch depends on <see cref="IImdbWatchlists"/> only.
/// Delegates to <see cref="WatchlistService"/>, which selects a provider by access mode.
/// </summary>
public sealed class ImdbWatchlistsClient(WatchlistService watchlistService) : IImdbWatchlists
{
    public Task<Watchlist> GetListAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        return watchlistService.GetListAsync(request, cancellationToken);
    }
}
