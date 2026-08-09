using ImdbMovieCatalog.Models;

namespace ImdbMovieCatalog.Repositories;

public class SqliteWatchlistRepository : IWatchlistRepository
{
    public Task<Watchlist?> GetAsync(
        string id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(
        Watchlist watchlist,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
