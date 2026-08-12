using ImdbWatchlists.Browser;
using ImdbWatchlists.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImdbWatchlists.Tests.Integration;

public sealed class ImdbBrowserFixture : IAsyncLifetime
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IPlaywright? _playwright;
    private PlaywrightBrowserManager? _browserManager;

    public async Task<IBrowserManager> GetBrowserManagerAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _playwright ??= await Playwright.CreateAsync();
            return _browserManager ??= new PlaywrightBrowserManager(
                _playwright,
                Microsoft.Extensions.Options.Options.Create(new ImdbWatchlistsOptions
                {
                    BrowserProfileDirectory = Path.Combine(
                        Path.GetTempPath(), "whattowatch-imdb-profile"),
                    StorageStatePath = Path.Combine(
                        Path.GetTempPath(), "whattowatch-imdb-profile", "imdb-session.json"),
                }),
                NullLogger<PlaywrightBrowserManager>.Instance);
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_browserManager is not null)
        {
            await _browserManager.DisposeAsync();
        }

        _playwright?.Dispose();
        _lock.Dispose();
    }
}
