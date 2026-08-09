using ImdbMovieCatalog.Models;

namespace ImdbMovieCatalog.Services;

public interface IWatchlistService
{
    Task<Watchlist> GetWatchlistAsync(
        CancellationToken cancellationToken);

    Task RefreshWatchlistAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Movie>> SearchAsync(
        MovieFilter filter,
        CancellationToken cancellationToken);

    Task<Movie?> GetRandomMovieAsync(
        MovieFilter filter,
        CancellationToken cancellationToken);
}
