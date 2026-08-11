using ImdbWatchlists.Options;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImdbWatchlists.Browser;

public sealed class PlaywrightBrowserManager(
    IPlaywright playwright,
    IOptions<ImdbWatchlistsOptions> options)
    : IBrowserManager, IAsyncDisposable
{
    private readonly ImdbWatchlistsOptions _options = options.Value;
    private readonly SemaphoreSlim _contextLock = new(1, 1);
    private IBrowserContext? _context;

    public async Task<IBrowserContext> GetContextAsync(CancellationToken cancellationToken = default)
    {
        if (_context is not null)
        {
            return _context;
        }

        await _contextLock.WaitAsync(cancellationToken);
        try
        {
            if (_context is null)
            {
                Directory.CreateDirectory(_options.BrowserProfileDirectory);

                _context = await playwright.Chromium.LaunchPersistentContextAsync(
                    _options.BrowserProfileDirectory,
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Channel = "chromium",
                        Headless = _options.BrowserHeadless,
                        Locale = "en-US",
                    }).WaitAsync(cancellationToken);
            }

            return _context;
        }
        finally
        {
            _contextLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
        }

        _contextLock.Dispose();
    }
}
