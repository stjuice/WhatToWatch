using ImdbWatchlists;
using ImdbWatchlists.Extraction;
using ImdbWatchlists.Models;
using ImdbWatchlists.Parsing;
using WhatToWatch.DTOs;
using WhatToWatch.Media;
using WhatToWatch.Repositories;

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
            return MediaCategoryRules.WithMoviesOnly(cached);

        return MediaCategoryRules.WithMoviesOnly(
            await FetchAndSaveAsync(listUrl, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Watchlist> ImportFromImdbPayloadAsync(
        ExtractedWatchlistPage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listId = request.ListId.Trim();
        var existing = await repository
            .GetAsync(listId, cancellationToken)
            .ConfigureAwait(false);

        var watchlist = ImdbExtractedPageMapper.ToImportWatchlist(request, existing);
        await repository.SaveAsync(watchlist, cancellationToken).ConfigureAwait(false);
        return MediaCategoryRules.WithMoviesOnly(watchlist);
    }

    public async Task<Watchlist?> GetWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var watchlist = await repository
            .GetAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return watchlist is null
            ? null
            : MediaCategoryRules.WithMoviesOnly(watchlist);
    }

    public async Task<IReadOnlyCollection<Watchlist>> GetWatchlistsAsync(
        CancellationToken cancellationToken = default)
    {
        var watchlists = await repository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. watchlists.Select(MediaCategoryRules.WithMoviesOnly)];
    }

    public async Task<Watchlist?> UpdateWatchlistAsync(
        string id,
        UpdateWatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var existing = await repository
            .GetAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            return null;

        var updated = existing with { Name = request.Name.Trim() };
        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return MediaCategoryRules.WithMoviesOnly(updated);
    }

    public Task<bool> DeleteWatchlistAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return repository.DeleteAsync(id, cancellationToken);
    }

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

        return MediaCategoryRules.WithMoviesOnly(
            await FetchAndSaveAsync(existing.Url, cancellationToken).ConfigureAwait(false));
    }

    private async Task<Watchlist> FetchAndSaveAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var watchlist = await imdbWatchlists
            .GetListAsync(new WatchlistRequest { Url = url }, cancellationToken)
            .ConfigureAwait(false);

        await repository.SaveAsync(watchlist, cancellationToken).ConfigureAwait(false);
        return watchlist;
    }
}
