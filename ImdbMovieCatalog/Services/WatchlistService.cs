using ImdbMovieCatalog.Models;

namespace ImdbMovieCatalog.Services;

public class WatchlistService : IWatchlistService
{
    public Task<Watchlist> GetWatchlistAsync(
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RefreshWatchlistAsync(
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Movie>> SearchAsync(
        MovieFilter filter,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Movie?> GetRandomMovieAsync(
        MovieFilter filter,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
