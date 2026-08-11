using ImdbWatchlists.Models;
using ImdbWatchlists.Options;
using Microsoft.Extensions.Options;

namespace ImdbWatchlists.Repositories;

public class SqliteWatchlistRepository(IOptions<ImdbWatchlistsOptions> options) : IWatchlistRepository
{
    private readonly ImdbWatchlistsOptions _options = options.Value;

    public Task<Watchlist?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Watchlist>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(
        Watchlist watchlist,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
