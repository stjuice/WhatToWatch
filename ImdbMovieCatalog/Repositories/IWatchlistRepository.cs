using ImdbMovieCatalog.Models;

namespace ImdbMovieCatalog.Repositories;

public interface IWatchlistRepository
{
    Task<Watchlist?> GetAsync(
        string id,
        CancellationToken cancellationToken);

    Task SaveAsync(
        Watchlist watchlist,
        CancellationToken cancellationToken);
}
