using System.Text.Json;
using ImdbWatchlists.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImdbWatchlists.Browser;

public sealed class PlaywrightBrowserManager(
    IPlaywright playwright,
    IOptions<ImdbWatchlistsOptions> options,
    ILogger<PlaywrightBrowserManager> logger)
    : IBrowserManager, IAsyncDisposable
{
    private const float LaunchTimeoutMs = 60_000;
    private const float OperationTimeoutMs = 30_000;
    private const float NavigationTimeoutMs = 60_000;
    private static readonly TimeSpan SessionFileTimeout = TimeSpan.FromSeconds(15);

    private static readonly JsonSerializerOptions SessionJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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
                var isNewProfile = !Directory.Exists(_options.BrowserProfileDirectory)
                    || !Directory.EnumerateFileSystemEntries(_options.BrowserProfileDirectory).Any();

                Directory.CreateDirectory(_options.BrowserProfileDirectory);

                var context = await playwright.Chromium.LaunchPersistentContextAsync(
                    _options.BrowserProfileDirectory,
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Channel = "chromium",
                        Headless = _options.BrowserHeadless,
                        Locale = "en-US",
                        Timeout = LaunchTimeoutMs,
                    }).WaitAsync(cancellationToken);

                context.SetDefaultTimeout(OperationTimeoutMs);
                context.SetDefaultNavigationTimeout(NavigationTimeoutMs);

                if (_options.BlockNonEssentialResources)
                {
                    await context.RouteAsync("**/*", BlockNonEssentialResourceAsync)
                        .WaitAsync(cancellationToken);
                }

                if (isNewProfile)
                {
                    await SeedSessionCookiesAsync(context, cancellationToken);
                }

                _context = context;
            }

            return _context;
        }
        finally
        {
            _contextLock.Release();
        }
    }

    public async Task PersistStorageStateAsync(CancellationToken cancellationToken = default)
    {
        var context = _context;
        if (context is null || string.IsNullOrWhiteSpace(_options.StorageStatePath))
            return;

        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_options.StorageStatePath));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await context.StorageStateAsync(new BrowserContextStorageStateOptions
            {
                Path = _options.StorageStatePath,
            }).WaitAsync(SessionFileTimeout, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to persist IMDb Playwright session to {StorageStatePath}",
                _options.StorageStatePath);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await PersistStorageStateAsync(CancellationToken.None);

        if (_context is not null)
        {
            try
            {
                await _context.CloseAsync().WaitAsync(SessionFileTimeout);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to close Playwright browser context during dispose");
            }
        }

        _contextLock.Dispose();
    }

    private static async Task BlockNonEssentialResourceAsync(IRoute route)
    {
        switch (route.Request.ResourceType)
        {
            case "image":
            case "media":
            case "font":
            case "stylesheet":
                await route.AbortAsync();
                break;

            default:
                await route.FallbackAsync();
                break;
        }
    }

    private async Task SeedSessionCookiesAsync(
        IBrowserContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.StorageStatePath)
            || !File.Exists(_options.StorageStatePath))
        {
            return;
        }

        try
        {
            await using var stream = File.OpenRead(_options.StorageStatePath);
            var session = await JsonSerializer
                .DeserializeAsync<SessionState>(stream, SessionJsonOptions, cancellationToken);

            if (session?.Cookies is not { Count: > 0 } cookies)
                return;

            await context.AddCookiesAsync(cookies.Select(ToCookie))
                .WaitAsync(SessionFileTimeout, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to seed IMDb session cookies from {StorageStatePath}",
                _options.StorageStatePath);
        }
    }

    private static Cookie ToCookie(SessionCookie cookie) => new()
    {
        Name = cookie.Name,
        Value = cookie.Value,
        Domain = cookie.Domain,
        Path = cookie.Path,
        Expires = cookie.Expires is null ? null : (float)cookie.Expires,
        HttpOnly = cookie.HttpOnly,
        Secure = cookie.Secure,
        SameSite = cookie.SameSite switch
        {
            "Strict" => SameSiteAttribute.Strict,
            "Lax" => SameSiteAttribute.Lax,
            "None" => SameSiteAttribute.None,
            _ => null,
        },
    };

    private sealed record SessionState(List<SessionCookie>? Cookies);

    private sealed record SessionCookie(
        string Name,
        string Value,
        string? Domain,
        string? Path,
        double? Expires,
        bool? HttpOnly,
        bool? Secure,
        string? SameSite);
}
