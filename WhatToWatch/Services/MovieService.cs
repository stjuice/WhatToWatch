using ImdbWatchlists.Models;
using System.Threading;
using WhatToWatch.Media;
using WhatToWatch.Models;
using WhatToWatch.Repositories;

namespace WhatToWatch.Services;

public class MovieService(
    IWatchlistRepository repository,
    IRandomizationService randomizationService,
    IWatchlistService watchlistService) : IMovieService
{
    public async Task<Movie?> GetMovieAsync(
        string watchlistId,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(watchlistId);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var watchlist = await GetPopulatedWatchlistAsync(
            watchlistId,
            cancellationToken).ConfigureAwait(false);

        return watchlist?.Movies.FirstOrDefault(movie =>
            MediaCategoryRules.IsMovie(movie) &&
            movie.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyCollection<Movie>?> GetMoviesAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var movieSet = await ResolveMoviesAsync(
            filter.WatchlistId,
            cancellationToken).ConfigureAwait(false);

        if (movieSet is null)
            return null;

        var movies = movieSet.Select(reference => reference.Movie).ToArray();

        return randomizationService.Filter(movies, filter);
    }

    public Task<IReadOnlyCollection<MovieReference>?> GetMovieSetAsync(
        string? watchlistId,
        CancellationToken cancellationToken = default) =>
        ResolveMoviesAsync(watchlistId, cancellationToken);

    public async Task<Movie?> GetRandomMovieAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var movieSet = await ResolveMoviesAsync(
            filter.WatchlistId,
            cancellationToken).ConfigureAwait(false);
        if (movieSet is null)
            return null;

        var movies = movieSet.Select(reference => reference.Movie).ToArray();
        var filtered = randomizationService.Filter(movies, filter);

        return randomizationService.PickRandom(filtered);
    }

    private async Task<IReadOnlyCollection<MovieReference>?> ResolveMoviesAsync(
        string? watchlistId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(watchlistId))
            return await GetAllMovies(cancellationToken);

        var watchlist = await GetPopulatedWatchlistAsync(
            watchlistId,
            cancellationToken).ConfigureAwait(false);

        if (watchlist is null)
            return null;

        return
            [
                .. MediaCategoryRules.MoviesOnly(watchlist.Movies)
                    .Select(movie => new MovieReference(watchlist.Id, movie)),
            ];
    }

    private async Task<IReadOnlyCollection<MovieReference>?> GetAllMovies(CancellationToken cancellationToken)
    {
        var watchlists = await repository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        return
            [
                .. watchlists.SelectMany(watchlist =>MediaCategoryRules.MoviesOnly(watchlist.Movies)
                    .Select(movie => new MovieReference(watchlist.Id, movie))),
            ];
    }

    private async Task<Watchlist?> GetPopulatedWatchlistAsync(
        string watchlistId,
        CancellationToken cancellationToken)
    {
        var watchlist = await repository
            .GetAsync(watchlistId, cancellationToken)
            .ConfigureAwait(false);

        if (watchlist is null || watchlist.Movies.Count > 0)
            return watchlist;

        return await watchlistService
            .RefreshWatchlistAsync(watchlistId, cancellationToken)
            .ConfigureAwait(false);
    }
}
