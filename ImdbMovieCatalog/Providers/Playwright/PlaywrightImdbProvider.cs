using ImdbMovieCatalog.Models;

namespace ImdbMovieCatalog.Providers.Playwright;

public class PlaywrightImdbProvider : IWatchlistProvider
{
    public Task<IReadOnlyCollection<Movie>> GetWatchlistAsync(
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
