using ImdbWatchlists.Browser;
using ImdbWatchlists.Options;
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

        var options = Microsoft.Extensions.Options.Options.Create(new ImdbWatchlistsOptions
        {
            BrowserProfileDirectory = profileDirectory,
        });

        await using var manager = new PlaywrightBrowserManager(playwright.Object, options);

        var first = await manager.GetContextAsync();
        var second = await manager.GetContextAsync();

        Assert.Same(first, second);
        chromium.Verify(
            item => item.LaunchPersistentContextAsync(
                profileDirectory,
                It.IsAny<BrowserTypeLaunchPersistentContextOptions>()),
            Times.Once);
    }
}
