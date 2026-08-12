using ImdbWatchlists;
using ImdbWatchlists.Models;
using ImdbWatchlists.Parsing;
using WhatToWatch.Mapping;
using WhatToWatch.Repositories;
using Watchlist = WhatToWatch.Models.Watchlist;

namespace WhatToWatch.Services;

public class WatchlistService(
    IWatchlistRepository repository,
    IImdbWatchlists imdbWatchlists) : IWatchlistService
{
    public async Task<Watchlist> ImportAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var listUrl = ImdbListUrl.Normalize(url);

        var cached = await repository
            .GetByUrlAsync(listUrl, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
            return cached;

        return await FetchAndSaveAsync(listUrl, cancellationToken).ConfigureAwait(false);
    }

    public Task<Watchlist?> GetWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return repository.GetAsync(id, cancellationToken);
    }

    public Task<IReadOnlyCollection<Watchlist>> GetWatchlistsAsync(
        CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public async Task<Watchlist?> RefreshWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var existing = await repository
            .GetAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            return null;

        if (string.IsNullOrWhiteSpace(existing.Url))
            throw new InvalidOperationException(
                $"Watchlist '{id}' has no URL to refresh from.");

        return await FetchAndSaveAsync(existing.Url, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Watchlist> FetchAndSaveAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var imdbWatchlist = await imdbWatchlists
            .GetListAsync(new WatchlistRequest { Url = url }, cancellationToken)
            .ConfigureAwait(false);

        var watchlist = MovieMapper.ToApp(imdbWatchlist);
        await repository.SaveAsync(watchlist, cancellationToken).ConfigureAwait(false);
        return watchlist;
    }
}
