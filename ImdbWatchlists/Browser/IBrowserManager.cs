using Microsoft.Playwright;

namespace ImdbWatchlists.Browser;

public interface IBrowserManager
{
    Task<IBrowserContext> GetContextAsync(CancellationToken cancellationToken = default);

    Task PersistStorageStateAsync(CancellationToken cancellationToken = default);
}
