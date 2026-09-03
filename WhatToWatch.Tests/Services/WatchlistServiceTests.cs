using ImdbWatchlists;
using ImdbWatchlists.Extraction;
using ImdbWatchlists.Models;
using Moq;
using WhatToWatch.Repositories;
using WhatToWatch.Services;

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
        var expected = CreateWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetWatchlistAsync(ListId);

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.Name, result.Name);
        Assert.Single(result.Movies);
        Assert.Equal("tt1", result.Movies.First().Id);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWatchlistAsync_ReturnsMoviesOnly_WhenWatchlistContainsMixedTypes()
    {
        var mixed = CreateWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow) with
        {
            Movies =
            [
                new Movie
                {
                    Id = "tt1",
                    Title = "Movie",
                    MediaCategory = MediaCategory.Movie,
                    Genres = ["Drama"],
                },
                new Movie
                {
                    Id = "tt2",
                    Title = "Series",
                    MediaCategory = MediaCategory.TvShow,
                    Genres = ["Drama"],
                },
                new Movie
                {
                    Id = "tt3",
                    Title = "Legacy",
                    MediaCategory = MediaCategory.Unknown,
                    Genres = ["Drama"],
                },
            ],
        };
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mixed);

        var result = await CreateSut().GetWatchlistAsync(ListId);

        Assert.Equal(["tt1", "tt3"], result!.Movies.Select(movie => movie.Id));
    }

    [Fact]
    public async Task GetWatchlistsAsync_ReturnsAllWatchlists()
    {
        IReadOnlyCollection<Watchlist> expected =
        [
            CreateWatchlist("ls1", refreshedAt: DateTimeOffset.UtcNow),
            CreateWatchlist("ls2", refreshedAt: DateTimeOffset.UtcNow),
        ];
        _repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetWatchlistsAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, watchlist => Assert.Single(watchlist.Movies));
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_ReturnsCached_WhenAlreadyStored()
    {
        var cached = CreateWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await CreateSut().ImportAsync(ListUrl);

        Assert.Equal(cached.Id, result.Id);
        Assert.Single(result.Movies);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<Watchlist>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_FetchesAndSaves_WhenMissing()
    {
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Watchlist?)null);
        _imdb
            .Setup(i => i.GetListAsync(
                It.Is<WatchlistRequest>(r => r.Url == ListUrl),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateImdbWatchlist(ListId));

        var result = await CreateSut().ImportAsync(ListUrl);

        Assert.Equal(ListId, result.Id);
        Assert.Equal("Favourites", result.Name);
        Assert.Single(result.Movies);
        _repository.Verify(
            r => r.SaveAsync(
                It.Is<Watchlist>(w => w.Id == ListId && w.Movies.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportAsync_PersistsAllTypes_ButReturnsMoviesOnly()
    {
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Watchlist?)null);
        _imdb
            .Setup(i => i.GetListAsync(
                It.IsAny<WatchlistRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateImdbWatchlist(ListId) with
            {
                Movies =
                [
                    new Movie
                    {
                        Id = "tt0133093",
                        Title = "The Matrix",
                        MediaCategory = MediaCategory.Movie,
                        Genres = ["Action"],
                    },
                    new Movie
                    {
                        Id = "tt0903747",
                        Title = "Breaking Bad",
                        MediaCategory = MediaCategory.TvShow,
                        Genres = ["Drama"],
                    },
                ],
            });

        Watchlist? saved = null;
        _repository
            .Setup(r => r.SaveAsync(It.IsAny<Watchlist>(), It.IsAny<CancellationToken>()))
            .Callback<Watchlist, CancellationToken>((watchlist, _) => saved = watchlist)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().ImportAsync(ListUrl);

        Assert.NotNull(saved);
        Assert.Equal(2, saved.Movies.Count);
        var returned = Assert.Single(result.Movies);
        Assert.Equal("tt0133093", returned.Id);
    }

    [Fact]
    public async Task ImportAsync_ReturnsCachedWithoutScraping_WhenStoredCopyIsOld()
    {
        var old = CreateWatchlist(
            ListId,
            refreshedAt: DateTimeOffset.UtcNow - TimeSpan.FromDays(400));
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(old);

        var result = await CreateSut().ImportAsync(ListUrl);

        Assert.Equal(old.Id, result.Id);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<Watchlist>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_CallsImdbWatchlistsAndPersistsResult()
    {
        var existing = CreateWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
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
            r => r.SaveAsync(It.IsAny<Watchlist>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_UsesPublicAccess_ForMvp()
    {
        var existing = CreateWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
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
            .ReturnsAsync((Watchlist?)null);

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
            .ReturnsAsync((Watchlist?)null);

        var result = await CreateSut().ImportFromImdbPayloadAsync(
            new ExtractedWatchlistPage
            {
                ListId = ListId,
                Title = "Sci-Fi",
                Url = ListUrl,
                HasNextPage = false,
                Movies =
                [
                    new ExtractedMoviePage
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
                        TitleType = "movie",
                    },
                    new ExtractedMoviePage
                    {
                        ImdbId = "tt0133093",
                        Title = "The Matrix Duplicate",
                        Year = 1999,
                        ImageUrl = null,
                        TitleType = "movie",
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
                It.Is<Watchlist>(w => w.Id == ListId && w.Movies.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportFromImdbPayloadAsync_TreatsTypelessTitlesAsMovies()
    {
        Watchlist? saved = null;
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Watchlist?)null);
        _repository
            .Setup(r => r.SaveAsync(It.IsAny<Watchlist>(), It.IsAny<CancellationToken>()))
            .Callback<Watchlist, CancellationToken>((watchlist, _) => saved = watchlist)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().ImportFromImdbPayloadAsync(
            new ExtractedWatchlistPage
            {
                ListId = ListId,
                Title = "Sci-Fi",
                HasNextPage = false,
                Movies =
                [
                    new ExtractedMoviePage
                    {
                        ImdbId = "tt0133093",
                        Title = "The Matrix",
                    },
                ],
            });

        Assert.NotNull(saved);
        Assert.Single(saved.Movies);
        Assert.Equal(MediaCategory.Unknown, saved.Movies.First().MediaCategory);
        var returned = Assert.Single(result.Movies);
        Assert.Equal("tt0133093", returned.Id);
    }

    [Fact]
    public async Task UpdateWatchlistAsync_RenamesExistingWatchlist()
    {
        var existing = CreateWatchlist(ListId, refreshedAt: DateTimeOffset.UtcNow);
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
                It.Is<Watchlist>(w => w.Id == ListId && w.Name == "Renamed"),
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

    private static Watchlist CreateWatchlist(
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
                new Movie
                {
                    Id = "tt1",
                    Title = "Cached Movie",
                    Year = 2000,
                    Genres = ["Drama"],
                    MediaCategory = MediaCategory.Movie,
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
                    MediaCategory = MediaCategory.Movie,
                },
            ],
        };
}
