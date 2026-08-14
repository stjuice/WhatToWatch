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

    [Fact]
    public async Task ImportFromImdbPayloadAsync_SavesNormalizedWatchlist_WithoutServerScrape()
    {
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppWatchlist?)null);

        var result = await CreateSut().ImportFromImdbPayloadAsync(
            new WhatToWatch.DTOs.ImportImdbWatchlistRequest
            {
                ListId = ListId,
                Title = "Sci-Fi",
                Url = ListUrl,
                Movies =
                [
                    new WhatToWatch.DTOs.ImportImdbMovieRequest
                    {
                        ImdbId = "tt0133093",
                        Title = "The Matrix",
                        Year = 1999,
                        ImageUrl = "https://example.com/m.jpg",
                        Rating = 8.7,
                        Plot = "A computer hacker learns from mysterious rebels.",
                        RuntimeMinutes = 136,
                        Director = "Lana Wachowski",
                        Genres = ["Action", "Sci-Fi"],
                    },
                    new WhatToWatch.DTOs.ImportImdbMovieRequest
                    {
                        ImdbId = "tt0133093",
                        Title = "The Matrix Duplicate",
                        Year = 1999,
                        ImageUrl = null,
                    },
                ],
            });

        var movie = Assert.Single(result.Movies);
        Assert.Equal(ListId, result.Id);
        Assert.Equal("Sci-Fi", result.Name);
        Assert.Equal(ListUrl, result.Url);
        Assert.Equal("tt0133093", movie.Id);
        Assert.Equal(8.7, movie.Rating);
        Assert.Equal(["Action", "Sci-Fi"], movie.Genres);
        _repository.Verify(
            r => r.SaveAsync(
                It.Is<AppWatchlist>(w => w.Id == ListId && w.Movies.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateWatchlistAsync_RenamesExistingWatchlist()
    {
        var existing = CreateAppWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateSut().UpdateWatchlistAsync(
            ListId,
            new WhatToWatch.DTOs.UpdateWatchlistRequest { Name = "Renamed" });

        Assert.NotNull(result);
        Assert.Equal("Renamed", result.Name);
        _repository.Verify(
            r => r.SaveAsync(
                It.Is<AppWatchlist>(w => w.Id == ListId && w.Name == "Renamed"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteWatchlistAsync_DelegatesToRepository()
    {
        _repository
            .Setup(r => r.DeleteAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Assert.True(await CreateSut().DeleteWatchlistAsync(ListId));
        _repository.Verify(
            r => r.DeleteAsync(ListId, It.IsAny<CancellationToken>()),
            Times.Once);
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
                    Plot = "A computer hacker learns from mysterious rebels.",
                    RuntimeMinutes = 136,
                    Director = "Lana Wachowski",
                    Genres = ["Action", "Sci-Fi"],
                },
            ],
        };
}
