using ImdbWatchlists.Models;

namespace ImdbWatchlists.Providers;

/// <summary>
/// MVP provider: fetches publicly accessible IMDb lists over HTTP (no browser).
/// </summary>
public class PublicWatchlistProvider(HttpClient httpClient) : IWatchlistProvider
{
    private readonly HttpClient _httpClient = httpClient;

    public WatchlistAccess Access => WatchlistAccess.Public;

    public Task<Watchlist> GetWatchlistAsync(
        WatchlistRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
