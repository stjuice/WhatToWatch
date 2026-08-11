using ImdbWatchlists.Repositories;
using WhatToWatch.Mapping;
using WhatToWatch.Models;

namespace WhatToWatch.Services;

public class MovieService(IWatchlistRepository repository) : IMovieService
{
    public Task<Movie?> GetMovieAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Movie>> GetMoviesAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Movie?> GetRandomMovieAsync(
        MovieFilter filter,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
