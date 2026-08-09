using ImdbMovieCatalog.Models;

namespace ImdbMovieCatalog.Providers;

public interface IWatchlistProvider
{
    Task<IReadOnlyCollection<Movie>> GetWatchlistAsync(
        CancellationToken cancellationToken);
}
