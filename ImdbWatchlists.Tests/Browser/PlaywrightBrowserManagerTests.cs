using ImdbWatchlists.Browser;
using ImdbWatchlists.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Moq;

namespace ImdbWatchlists.Tests.Browser;

public class PlaywrightBrowserManagerTests
{
    [Fact]
    public async Task GetContextAsync_CreatesOnePersistentContextAndReusesIt()
    {
        var playwright = new Mock<IPlaywright>();
        var chromium = new Mock<IBrowserType>();
        var context = new Mock<IBrowserContext>();
        var profileDirectory = Path.Combine(
            Path.GetTempPath(),
            $"whattowatch-test-{Guid.NewGuid():N}");

        playwright.SetupGet(item => item.Chromium).Returns(chromium.Object);
        chromium.Setup(item => item.LaunchPersistentContextAsync(
                It.IsAny<string>(),
                It.IsAny<BrowserTypeLaunchPersistentContextOptions>()))
            .ReturnsAsync(context.Object);
        context.Setup(item => item.RouteAsync(
                It.IsAny<string>(),
                It.IsAny<Func<IRoute, Task>>(),
                It.IsAny<BrowserContextRouteOptions>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        var options = Microsoft.Extensions.Options.Options.Create(new ImdbWatchlistsOptions
        {
            BrowserProfileDirectory = profileDirectory,
            StorageStatePath = Path.Combine(profileDirectory, "imdb-session.json"),
        });

        await using var manager = new PlaywrightBrowserManager(
            playwright.Object,
            options,
            NullLogger<PlaywrightBrowserManager>.Instance);

        var first = await manager.GetContextAsync();
        var second = await manager.GetContextAsync();

        Assert.Same(first, second);
        chromium.Verify(
            item => item.LaunchPersistentContextAsync(
                profileDirectory,
                It.IsAny<BrowserTypeLaunchPersistentContextOptions>()),
            Times.Once);

        Cleanup(profileDirectory);
    }

    [Fact]
    public async Task GetContextAsync_SeedsCookiesFromSessionFileForNewProfile()
    {
        var playwright = new Mock<IPlaywright>();
        var chromium = new Mock<IBrowserType>();
        var context = new Mock<IBrowserContext>();
        var root = Path.Combine(Path.GetTempPath(), $"whattowatch-test-{Guid.NewGuid():N}");
        var profileDirectory = Path.Combine(root, "profile");
        var storageStatePath = Path.Combine(root, "imdb-session.json");

        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(
            storageStatePath,
            """
            {
              "cookies": [
                {
                  "name": "session-id",
                  "value": "abc",
                  "domain": ".imdb.com",
                  "path": "/",
                  "expires": 1893456000,
                  "httpOnly": false,
                  "secure": true,
                  "sameSite": "Lax"
                }
              ],
              "origins": []
            }
            """);

        IEnumerable<Cookie>? seededCookies = null;
        playwright.SetupGet(item => item.Chromium).Returns(chromium.Object);
        chromium.Setup(item => item.LaunchPersistentContextAsync(
                It.IsAny<string>(),
                It.IsAny<BrowserTypeLaunchPersistentContextOptions>()))
            .ReturnsAsync(context.Object);
        context.Setup(item => item.RouteAsync(
                It.IsAny<string>(),
                It.IsAny<Func<IRoute, Task>>(),
                It.IsAny<BrowserContextRouteOptions>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        context.Setup(item => item.AddCookiesAsync(It.IsAny<IEnumerable<Cookie>>()))
            .Callback<IEnumerable<Cookie>>(cookies => seededCookies = [.. cookies])
            .Returns(Task.CompletedTask);

        var options = Microsoft.Extensions.Options.Options.Create(new ImdbWatchlistsOptions
        {
            BrowserProfileDirectory = profileDirectory,
            StorageStatePath = storageStatePath,
        });

        await using var manager = new PlaywrightBrowserManager(
            playwright.Object,
            options,
            NullLogger<PlaywrightBrowserManager>.Instance);
        await manager.GetContextAsync();

        Assert.NotNull(seededCookies);
        var cookie = Assert.Single(seededCookies!);
        Assert.Equal("session-id", cookie.Name);
        Assert.Equal(".imdb.com", cookie.Domain);
        Assert.Equal(SameSiteAttribute.Lax, cookie.SameSite);

        Cleanup(root);
    }

    private static void Cleanup(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for temp test files.
        }
    }
}
