using WhatToWatch.Models;

namespace WhatToWatch.Services;

public interface IMovieService
{
    Task<Movie?> GetMovieAsync(
        string watchlistId,
        string id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Movie>?> GetMoviesAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default);

    Task<Movie?> GetRandomMovieAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default);
}
