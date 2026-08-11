using ImdbWatchlists;
using ImdbWatchlists.Models;
using Moq;
using WhatToWatch.Repositories;
using WhatToWatch.Services;
using AppMovie = WhatToWatch.Models.Movie;
using AppWatchlist = WhatToWatch.Models.Watchlist;

namespace WhatToWatch.Tests.Services;

public class WatchlistServiceTests
{
    private const string ListUrl = "https://www.imdb.com/list/ls055592025/";
    private const string ListId = "ls055592025";

    private readonly Mock<IWatchlistRepository> _repository = new();
    private readonly Mock<IImdbWatchlists> _imdb = new();

    private WatchlistService CreateSut() =>
        new(_repository.Object, _imdb.Object);

    [Fact]
    public async Task GetWatchlistAsync_ReturnsWatchlist_WhenIdExists()
    {
        var expected = CreateAppWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetWatchlistAsync(ListId);

        Assert.Same(expected, result);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWatchlistsAsync_ReturnsAllWatchlists()
    {
        IReadOnlyCollection<AppWatchlist> expected =
        [
            CreateAppWatchlist("ls1", refreshedAt: DateTimeOffset.UtcNow),
            CreateAppWatchlist("ls2", refreshedAt: DateTimeOffset.UtcNow),
        ];
        _repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetWatchlistsAsync();

        Assert.Equal(2, result.Count);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_ReturnsCached_WhenAlreadyStored()
    {
        var cached = CreateAppWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await CreateSut().ImportAsync(ListUrl);

        Assert.Same(cached, result);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<AppWatchlist>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_FetchesAndSaves_WhenMissing()
    {
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppWatchlist?)null);
        _imdb
            .Setup(i => i.GetListAsync(
                It.Is<WatchlistRequest>(r => r.Url == ListUrl),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateImdbWatchlist(ListId));

        var result = await CreateSut().ImportAsync(ListUrl);

        Assert.Equal(ListId, result.Id);
        Assert.Equal("Favourites", result.Name);
        _repository.Verify(
            r => r.SaveAsync(
                It.Is<AppWatchlist>(w => w.Id == ListId && w.Movies.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportAsync_ReturnsCachedWithoutScraping_WhenStoredCopyIsOld()
    {
        var old = CreateAppWatchlist(
            ListId,
            refreshedAt: DateTimeOffset.UtcNow - TimeSpan.FromDays(400));
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(old);

        var result = await CreateSut().ImportAsync(ListUrl);

        Assert.Same(old, result);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<AppWatchlist>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_CallsImdbWatchlistsAndPersistsResult()
    {
        var existing = CreateAppWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _imdb
            .Setup(i => i.GetListAsync(
                It.Is<WatchlistRequest>(r => r.Url == existing.Url),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateImdbWatchlist(ListId, name: "Updated"));

        var result = await CreateSut().RefreshWatchlistAsync(ListId);

        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<AppWatchlist>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_UsesPublicAccess_ForMvp()
    {
        var existing = CreateAppWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _imdb
            .Setup(i => i.GetListAsync(
                It.IsAny<WatchlistRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateImdbWatchlist(ListId));

        await CreateSut().RefreshWatchlistAsync(ListId);

        _imdb.Verify(
            i => i.GetListAsync(
                It.Is<WatchlistRequest>(r => r.Access == WatchlistAccess.Public),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_ReturnsNull_WhenWatchlistMissing()
    {
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppWatchlist?)null);

        var result = await CreateSut().RefreshWatchlistAsync(ListId);

        Assert.Null(result);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AppWatchlist CreateAppWatchlist(
        string id,
        DateTimeOffset? refreshedAt) =>
        new()
        {
            Id = id,
            Name = "Cached",
            Url = $"https://www.imdb.com/list/{id}/",
            LastRefreshedAt = refreshedAt,
            Movies =
            [
                new AppMovie
                {
                    Id = "tt1",
                    Title = "Cached Movie",
                    Year = 2000,
                    Genres = ["Drama"],
                },
            ],
        };

    private static Watchlist CreateImdbWatchlist(string id, string name = "Favourites") =>
        new()
        {
            Id = id,
            Name = name,
            Url = $"https://www.imdb.com/list/{id}/",
            LastRefreshedAt = DateTimeOffset.UtcNow,
            Movies =
            [
                new Movie
                {
                    Id = "tt0133093",
                    Title = "The Matrix",
                    Year = 1999,
                    Rating = 8.7,
                    Genres = ["Action", "Sci-Fi"],
                },
            ],
        };
}
