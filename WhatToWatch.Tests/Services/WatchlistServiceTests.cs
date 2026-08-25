using ImdbWatchlists;
using ImdbWatchlists.Models;
using Moq;
using WhatToWatch.Repositories;
using WhatToWatch.Services;
using MovieModel = WhatToWatch.Models.Movie;
using WatchlistModel = WhatToWatch.Models.Watchlist;

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
        var expected = CreateWatchlistModel(ListId, refreshedAt: DateTimeOffset.UtcNow);
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
        var mixed = CreateWatchlistModel(ListId, refreshedAt: DateTimeOffset.UtcNow) with
        {
            Movies =
            [
                new MovieModel
                {
                    Id = "tt1",
                    Title = "Movie",
                    MediaCategory = MediaCategory.Movie,
                    Genres = ["Drama"],
                },
                new MovieModel
                {
                    Id = "tt2",
                    Title = "Series",
                    MediaCategory = MediaCategory.TvShow,
                    Genres = ["Drama"],
                },
                new MovieModel
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
        IReadOnlyCollection<WatchlistModel> expected =
        [
            CreateWatchlistModel("ls1", refreshedAt: DateTimeOffset.UtcNow),
            CreateWatchlistModel("ls2", refreshedAt: DateTimeOffset.UtcNow),
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
        var cached = CreateWatchlistModel(ListId, refreshedAt: DateTimeOffset.UtcNow);
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
            r => r.SaveAsync(It.IsAny<WatchlistModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_FetchesAndSaves_WhenMissing()
    {
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WatchlistModel?)null);
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
                It.Is<WatchlistModel>(w => w.Id == ListId && w.Movies.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportAsync_PersistsAllTypes_ButReturnsMoviesOnly()
    {
        _repository
            .Setup(r => r.GetByUrlAsync(ListUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WatchlistModel?)null);
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

        WatchlistModel? saved = null;
        _repository
            .Setup(r => r.SaveAsync(It.IsAny<WatchlistModel>(), It.IsAny<CancellationToken>()))
            .Callback<WatchlistModel, CancellationToken>((watchlist, _) => saved = watchlist)
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
        var old = CreateWatchlistModel(
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
            r => r.SaveAsync(It.IsAny<WatchlistModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_CallsImdbWatchlistsAndPersistsResult()
    {
        var existing = CreateWatchlistModel(ListId, refreshedAt: DateTimeOffset.UtcNow);
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
            r => r.SaveAsync(It.IsAny<WatchlistModel>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_UsesPublicAccess_ForMvp()
    {
        var existing = CreateWatchlistModel(ListId, refreshedAt: DateTimeOffset.UtcNow);
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
            .ReturnsAsync((WatchlistModel?)null);

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
            .ReturnsAsync((WatchlistModel?)null);

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
                        TitleType = "movie",
                    },
                    new WhatToWatch.DTOs.ImportImdbMovieRequest
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
                It.Is<WatchlistModel>(w => w.Id == ListId && w.Movies.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _imdb.Verify(
            i => i.GetListAsync(It.IsAny<WatchlistRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportFromImdbPayloadAsync_TreatsTypelessTitlesAsMovies()
    {
        WatchlistModel? saved = null;
        _repository
            .Setup(r => r.GetAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WatchlistModel?)null);
        _repository
            .Setup(r => r.SaveAsync(It.IsAny<WatchlistModel>(), It.IsAny<CancellationToken>()))
            .Callback<WatchlistModel, CancellationToken>((watchlist, _) => saved = watchlist)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().ImportFromImdbPayloadAsync(
            new WhatToWatch.DTOs.ImportImdbWatchlistRequest
            {
                ListId = ListId,
                Title = "Sci-Fi",
                Movies =
                [
                    new WhatToWatch.DTOs.ImportImdbMovieRequest
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
        var existing = CreateWatchlistModel(ListId, refreshedAt: DateTimeOffset.UtcNow);
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
                It.Is<WatchlistModel>(w => w.Id == ListId && w.Name == "Renamed"),
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

    private static WatchlistModel CreateWatchlistModel(
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
                new MovieModel
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
