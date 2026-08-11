using ImdbWatchlists.Models;

namespace ImdbWatchlists.Repositories;

public interface IWatchlistRepository
{
    Task<Watchlist?> GetAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Watchlist>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Watchlist watchlist,
        CancellationToken cancellationToken = default);
}
