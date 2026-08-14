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
            movie.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyCollection<Movie>?> GetMoviesAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var movies = await ResolveMoviesAsync(filter, cancellationToken).ConfigureAwait(false);
        if (movies is null)
            return null;

        return randomizationService.Filter(movies, filter);
    }

    public async Task<Movie?> GetRandomMovieAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var movies = await ResolveMoviesAsync(filter, cancellationToken).ConfigureAwait(false);
        if (movies is null)
            return null;

        var filtered = randomizationService.Filter(movies, filter);
        return randomizationService.PickRandom(filtered);
    }

    private async Task<IReadOnlyCollection<Movie>?> ResolveMoviesAsync(
        MovieFilter filter,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.WatchlistId))
        {
            var watchlists = await repository
                .GetAllAsync(cancellationToken)
                .ConfigureAwait(false);

            return [.. watchlists.SelectMany(watchlist => watchlist.Movies)];
        }

        var watchlist = await GetPopulatedWatchlistAsync(
            filter.WatchlistId,
            cancellationToken).ConfigureAwait(false);

        return watchlist?.Movies;
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
