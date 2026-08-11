using ImdbWatchlists.Models;
using ImdbWatchlists.Providers;

namespace ImdbWatchlists.Services;

public class WatchlistService(IEnumerable<IWatchlistProvider> providers)
{
    private readonly IReadOnlyDictionary<WatchlistAccess, IWatchlistProvider> _providers = providers.ToDictionary(provider => provider.Access);

    public Task<Watchlist> GetListAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_providers.TryGetValue(request.Access, out var provider))
        {
            throw new NotSupportedException(
                $"No watchlist provider is registered for access '{request.Access}'.");
        }

        return provider.GetWatchlistAsync(request, cancellationToken);
    }
}
