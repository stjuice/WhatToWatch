using ImdbWatchlists;
using ImdbWatchlists.Models;
using ImdbWatchlists.Parsing;
using WhatToWatch.DTOs;
using WhatToWatch.Mapping;
using WhatToWatch.Repositories;
using Watchlist = WhatToWatch.Models.Watchlist;
using AppMovie = WhatToWatch.Models.Movie;

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

    public async Task<Watchlist> ImportFromImdbPayloadAsync(
        ImportImdbWatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);

        if (request.Movies is null || request.Movies.Count == 0)
            throw new ArgumentException("At least one movie is required.", nameof(request));

        var listId = request.ListId.Trim();
        var existing = await repository
            .GetAsync(listId, cancellationToken)
            .ConfigureAwait(false);

        var watchlist = new Watchlist
        {
            Id = listId,
            Name = request.Title.Trim(),
            Url = !string.IsNullOrWhiteSpace(request.Url)
                ? request.Url.Trim()
                : string.IsNullOrWhiteSpace(existing?.Url)
                    ? BuildListUrl(listId)
                    : existing.Url,
            LastRefreshedAt = DateTimeOffset.UtcNow,
            Movies = DeduplicateMovies(request.Movies),
        };

        await repository.SaveAsync(watchlist, cancellationToken).ConfigureAwait(false);
        return watchlist;
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
        return updated;
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

    private static string BuildListUrl(string listId)
    {
        if (listId.StartsWith("ur", StringComparison.OrdinalIgnoreCase))
            return $"https://www.imdb.com/user/{listId}/watchlist/";

        return $"https://www.imdb.com/list/{listId}/";
    }

    private static IReadOnlyCollection<AppMovie> DeduplicateMovies(
        IReadOnlyCollection<ImportImdbMovieRequest> movies)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<AppMovie>(movies.Count);

        foreach (var movie in movies)
        {
            if (string.IsNullOrWhiteSpace(movie.ImdbId) || string.IsNullOrWhiteSpace(movie.Title))
                continue;

            var id = movie.ImdbId.Trim();
            if (!seen.Add(id))
                continue;

            result.Add(new AppMovie
            {
                Id = id,
                Title = movie.Title.Trim(),
                Year = movie.Year,
                PosterUrl = string.IsNullOrWhiteSpace(movie.ImageUrl) ? null : movie.ImageUrl.Trim(),
                Rating = movie.Rating,
                Plot = string.IsNullOrWhiteSpace(movie.Plot) ? null : movie.Plot.Trim(),
                RuntimeMinutes = movie.RuntimeMinutes,
                Director = string.IsNullOrWhiteSpace(movie.Director) ? null : movie.Director.Trim(),
                Genres = NormalizeGenres(movie.Genres),
            });
        }

        if (result.Count == 0)
            throw new ArgumentException("At least one valid movie is required.");

        return result;
    }

    private static IReadOnlyCollection<string> NormalizeGenres(
        IReadOnlyCollection<string>? genres)
    {
        if (genres is null || genres.Count == 0)
            return [];

        return [.. genres
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Select(genre => genre.Trim())];
    }
}
