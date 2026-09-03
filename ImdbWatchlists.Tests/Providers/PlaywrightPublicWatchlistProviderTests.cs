using ImdbWatchlists.Browser;
using ImdbWatchlists.Extraction;
using ImdbWatchlists.Models;
using ImdbWatchlists.Options;
using ImdbWatchlists.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using Moq;

namespace ImdbWatchlists.Tests.Providers;

public class PlaywrightPublicWatchlistProviderTests
{
    private const string ListUrl = "https://www.imdb.com/list/ls055592025/";

    [Fact]
    public async Task GetWatchlistAsync_LoadsAndScrapesPage()
    {
        var fixture = new PlaywrightFixture(SinglePage);
        var provider = fixture.CreateProvider();

        var watchlist = await provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl });

        Assert.Equal("ls055592025", watchlist.Id);
        Assert.Single(watchlist.Movies);
        Assert.Equal("The Matrix", watchlist.Movies.First().Title);
        fixture.Page.Verify(
            page => page.GotoAsync(ListUrl, It.IsAny<PageGotoOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task GetWatchlistAsync_FollowsPages_UntilNoNextPage()
    {
        var fixture = new PlaywrightFixture(
            BuildPage("tt0000001", "First", hasNextPage: true),
            BuildPage("tt0000002", "Second", hasNextPage: false));
        var provider = fixture.CreateProvider();

        var watchlist = await provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl });

        Assert.Equal(["tt0000001", "tt0000002"], watchlist.Movies.Select(movie => movie.Id));
        Assert.Equal(
            [ListUrl, $"{ListUrl}?page=2"],
            fixture.RequestedUrls);
    }

    [Fact]
    public async Task GetWatchlistAsync_StopsAtFirstPage_WhenNoNextPage()
    {
        var fixture = new PlaywrightFixture(SinglePage);
        var provider = fixture.CreateProvider();

        await provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl });

        Assert.Equal([ListUrl], fixture.RequestedUrls);
    }

    [Fact]
    public async Task GetWatchlistAsync_ReusesSharedContext_AcrossRequests()
    {
        var fixture = new PlaywrightFixture(SinglePage);
        var provider = fixture.CreateProvider();

        await provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl });
        await provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl });

        fixture.BrowserManager.Verify(
            manager => manager.GetContextAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        fixture.Page.Verify(
            page => page.CloseAsync(It.IsAny<PageCloseOptions>()),
            Times.Exactly(2));
        fixture.Context.Verify(
            context => context.CloseAsync(It.IsAny<BrowserContextCloseOptions>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWatchlistAsync_ThrowsBeforeUsingBrowser_WhenUrlIsNotAList()
    {
        var fixture = new PlaywrightFixture(SinglePage);
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<ImdbWatchlistException>(() =>
            provider.GetWatchlistAsync(new WatchlistRequest { Url = "https://example.com/" }));

        fixture.BrowserManager.Verify(
            manager => manager.GetContextAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWatchlistAsync_Throws_WhenImdbReturnsError()
    {
        var fixture = new PlaywrightFixture(404, SinglePage);
        var provider = fixture.CreateProvider();

        var exception = await Assert.ThrowsAsync<ImdbWatchlistException>(() =>
            provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl }));

        Assert.Contains("404", exception.Message);
    }

    [Fact]
    public async Task GetWatchlistAsync_IncludesBodyPreview_WhenResponseIsNotSuccessful()
    {
        var fixture = new PlaywrightFixture(405, SinglePage);
        var provider = fixture.CreateProvider();

        var exception = await Assert.ThrowsAsync<ImdbWatchlistException>(() =>
            provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl }));

        Assert.Contains("405", exception.Message);
        Assert.Contains("<html><body>", exception.Message);
    }

    [Fact]
    public async Task GetWatchlistAsync_ExplainsServerBlock_WhenImdbReturns403()
    {
        var fixture = new PlaywrightFixture(403, SinglePage);
        var provider = fixture.CreateProvider();

        var exception = await Assert.ThrowsAsync<ImdbWatchlistException>(() =>
            provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl }));

        Assert.Contains("403", exception.Message);
        Assert.Contains("blocked", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("imdb-session.json", exception.Message);
        fixture.Page.Verify(
            page => page.GotoAsync(ListUrl, It.IsAny<PageGotoOptions>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task GetWatchlistAsync_ExplainsHumanVerification_WhenListDataNeverAppears()
    {
        var fixture = new PlaywrightFixture(200, listDataAvailable: false, SinglePage);
        var provider = fixture.CreateProvider();

        var exception = await Assert.ThrowsAsync<ImdbWatchlistException>(() =>
            provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl }));

        Assert.Contains("human verification", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("200", exception.Message);
    }

    [Fact]
    public async Task GetWatchlistAsync_ReadsPage_AfterBotChallengeIsReplacedByListData()
    {
        var fixture = new PlaywrightFixture(SinglePage);
        var provider = fixture.CreateProvider();

        var watchlist = await provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl });

        Assert.Equal("The Matrix", Assert.Single(watchlist.Movies).Title);
    }

    [Fact]
    public async Task GetWatchlistAsync_RelaunchesBrowser_WhenCachedContextIsClosed()
    {
        var fixture = new PlaywrightFixture(SinglePage);
        var closedContext = new Mock<IBrowserContext>();
        closedContext.Setup(item => item.NewPageAsync())
            .ThrowsAsync(new PlaywrightException(
                "Target page, context or browser has been closed"));

        fixture.BrowserManager
            .SetupSequence(manager => manager.GetContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(closedContext.Object)
            .ReturnsAsync(fixture.Context.Object);

        var provider = fixture.CreateProvider();

        var watchlist = await provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl });

        Assert.Equal("The Matrix", Assert.Single(watchlist.Movies).Title);
    }

    [Fact]
    public async Task GetWatchlistAsync_ReportsBrowserFailure_WhenPageCannotBeOpened()
    {
        var fixture = new PlaywrightFixture(SinglePage);
        fixture.Context.Setup(item => item.NewPageAsync())
            .ThrowsAsync(new PlaywrightException("Browser closed unexpectedly"));

        var provider = fixture.CreateProvider();

        var exception = await Assert.ThrowsAsync<ImdbWatchlistException>(() =>
            provider.GetWatchlistAsync(new WatchlistRequest { Url = ListUrl }));

        Assert.Contains("could not open a browser page", exception.Message);
    }

    [Fact]
    public void Access_IsPublic()
    {
        var fixture = new PlaywrightFixture(SinglePage);
        var provider = fixture.CreateProvider();

        Assert.Equal(WatchlistAccess.Public, provider.Access);
    }

    private static readonly ExtractedWatchlistPage SinglePage = BuildPage(
        "tt0133093",
        "The Matrix",
        hasNextPage: false,
        year: 1999);

    private static ExtractedWatchlistPage BuildPage(
        string movieId,
        string title,
        bool hasNextPage,
        int? year = null) =>
        new()
        {
            ListId = "ls055592025",
            Title = "My Favourites",
            Movies =
            [
                new ExtractedMoviePage
                {
                    ImdbId = movieId,
                    Title = title,
                    Year = year,
                },
            ],
            HasNextPage = hasNextPage,
            NextPageUrl = hasNextPage ? $"{ListUrl}?page=2" : null,
        };

    private sealed class PlaywrightFixture
    {
        private readonly Queue<ExtractedWatchlistPage> _pages;

        public PlaywrightFixture(params ExtractedWatchlistPage[] pages)
            : this(200, pages)
        {
        }

        public PlaywrightFixture(int status, params ExtractedWatchlistPage[] pages)
            : this(status, listDataAvailable: true, pages)
        {
        }

        public PlaywrightFixture(int status, bool listDataAvailable, params ExtractedWatchlistPage[] pages)
        {
            _pages = new Queue<ExtractedWatchlistPage>(pages);

            var response = new Mock<IResponse>();
            response.SetupGet(item => item.Status).Returns(status);
            response.SetupGet(item => item.Ok).Returns(status is >= 200 and < 300);
            response.SetupGet(item => item.Url).Returns(ListUrl);
            response.Setup(item => item.AllHeadersAsync())
                .ReturnsAsync(new Dictionary<string, string>
                {
                    ["content-type"] = "text/html",
                });

            var element = new Mock<IElementHandle>();

            BrowserManager.Setup(manager =>
                    manager.GetContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Context.Object);
            Context.Setup(item => item.NewPageAsync()).ReturnsAsync(Page.Object);
            Page.Setup(item => item.EvaluateAsync<string>(
                    "() => navigator.userAgent",
                    It.IsAny<object>()))
                .ReturnsAsync("Test Browser");
            Page.Setup(item =>
                    item.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
                .ReturnsAsync((string url, PageGotoOptions _) =>
                {
                    RequestedUrls.Add(url);
                    return response.Object;
                });

            var waitSetup = Page.Setup(item =>
                item.WaitForSelectorAsync(
                    "script#__NEXT_DATA__",
                    It.IsAny<PageWaitForSelectorOptions>()));

            if (listDataAvailable)
            {
                waitSetup.ReturnsAsync(element.Object);
            }
            else
            {
                waitSetup.ThrowsAsync(new TimeoutException("Timeout 30000ms exceeded."));
            }

            Page.Setup(item => item.ContentAsync())
                .ReturnsAsync("<html><body>preview</body></html>");

            PageExtractor
                .Setup(extractor => extractor.ExtractCurrentPageAsync(
                    It.IsAny<IPage>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                    _pages.Count > 1 ? _pages.Dequeue() : _pages.Peek());
        }

        public List<string> RequestedUrls { get; } = [];

        public Mock<IBrowserManager> BrowserManager { get; } = new();

        public Mock<IBrowserContext> Context { get; } = new();

        public Mock<IPage> Page { get; } = new();

        public Mock<IImdbPageExtractor> PageExtractor { get; } = new();

        public ImdbWatchlistsOptions Options { get; } = new()
        {
            BrowserHeadless = true,
        };

        public PlaywrightPublicWatchlistProvider CreateProvider() =>
            new(
                BrowserManager.Object,
                PageExtractor.Object,
                Microsoft.Extensions.Options.Options.Create(Options),
                NullLogger<PlaywrightPublicWatchlistProvider>.Instance);
    }
}
