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
        ArgumentException.ThrowIfNullOrWhiteSpace(filter.WatchlistId);

        var watchlist = await GetPopulatedWatchlistAsync(
            filter.WatchlistId,
            cancellationToken).ConfigureAwait(false);

        if (watchlist is null)
        {
            return null;
        }

        return randomizationService.Filter(watchlist.Movies, filter);
    }

    public async Task<Movie?> GetRandomMovieAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentException.ThrowIfNullOrWhiteSpace(filter.WatchlistId);

        var watchlist = await GetPopulatedWatchlistAsync(
            filter.WatchlistId,
            cancellationToken).ConfigureAwait(false);

        if (watchlist is null)
        {
            return null;
        }

        var filtered = randomizationService.Filter(watchlist.Movies, filter);
        return randomizationService.PickRandom(filtered);
    }

    private async Task<Watchlist?> GetPopulatedWatchlistAsync(
        string watchlistId,
        CancellationToken cancellationToken)
    {
        var watchlist = await repository
            .GetAsync(watchlistId, cancellationToken)
            .ConfigureAwait(false);

        if (watchlist is null || watchlist.Movies.Count > 0)
        {
            return watchlist;
        }

        return await watchlistService
            .RefreshWatchlistAsync(watchlistId, cancellationToken)
            .ConfigureAwait(false);
    }
}
