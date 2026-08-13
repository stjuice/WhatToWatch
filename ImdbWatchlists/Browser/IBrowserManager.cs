using Microsoft.Playwright;

namespace ImdbWatchlists.Browser;

public interface IBrowserManager
{
    bool UsesPersistentContext { get; }

    bool StorageStateLoaded { get; }

    Task<IBrowserContext> GetContextAsync(CancellationToken cancellationToken = default);

    Task PersistStorageStateAsync(CancellationToken cancellationToken = default);
}
