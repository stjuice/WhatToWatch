using ImdbWatchlists.Models;
using Moq;
using WhatToWatch.Models;
using WhatToWatch.Repositories;
using WhatToWatch.Services;
using Movie = WhatToWatch.Models.Movie;
using Watchlist = WhatToWatch.Models.Watchlist;

namespace WhatToWatch.Tests.Services;

public class MovieServiceTests
{
    private const string WatchlistId = "ls1";

    private readonly Mock<IWatchlistRepository> _repository = new();
    private readonly Mock<IWatchlistService> _watchlistService = new();
    private readonly RandomizationService _randomization = new(new Random(7));

    private MovieService CreateSut() =>
        new(_repository.Object, _randomization, _watchlistService.Object);

    [Fact]
    public async Task GetMovieAsync_ReturnsMovie_WhenIdExists()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist());

        var movie = await CreateSut().GetMovieAsync(WatchlistId, "tt2");

        Assert.NotNull(movie);
        Assert.Equal("Inception", movie.Title);
        Assert.DoesNotContain(
            typeof(MovieService).GetConstructors()[0].GetParameters(),
            p => p.ParameterType.Name.Contains("Imdb", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetMovieAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist());

        Assert.Null(await CreateSut().GetMovieAsync(WatchlistId, "missing"));
    }

    [Fact]
    public async Task GetMovieAsync_ReturnsNull_WhenTitleIsTvShow()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist() with
            {
                Movies =
                [
                    new Movie
                    {
                        Id = "tt99",
                        Title = "Breaking Bad",
                        MediaCategory = MediaCategory.TvShow,
                        Genres = ["Drama"],
                    },
                ],
            });

        Assert.Null(await CreateSut().GetMovieAsync(WatchlistId, "tt99"));
    }

    [Fact]
    public async Task GetMoviesAsync_FiltersMoviesByRequest()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist());

        var movies = await CreateSut().GetMoviesAsync(new MovieFilter
        {
            WatchlistId = WatchlistId,
            Genres = ["Romance"],
        });

        Assert.NotNull(movies);
        Assert.Single(movies);
        Assert.Equal("tt3", movies.First().Id);
    }

    [Fact]
    public async Task GetMoviesAsync_ExcludesTvAndUnknownTitles()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist() with
            {
                Movies =
                [
                    new Movie
                    {
                        Id = "tt1",
                        Title = "The Matrix",
                        Year = 1999,
                        Rating = 8.7,
                        Genres = ["Action"],
                        MediaCategory = MediaCategory.Movie,
                    },
                    new Movie
                    {
                        Id = "tt2",
                        Title = "Breaking Bad",
                        Year = 2008,
                        Rating = 9.5,
                        Genres = ["Drama"],
                        MediaCategory = MediaCategory.TvShow,
                    },
                    new Movie
                    {
                        Id = "tt3",
                        Title = "Legacy",
                        Year = 2000,
                        Rating = 7.0,
                        Genres = ["Drama"],
                        MediaCategory = MediaCategory.Unknown,
                    },
                ],
            });

        var movies = await CreateSut().GetMoviesAsync(new MovieFilter
        {
            WatchlistId = WatchlistId,
        });

        var movie = Assert.Single(movies!);
        Assert.Equal("tt1", movie.Id);
    }

    [Fact]
    public async Task GetMoviesAsync_ReturnsNull_WhenWatchlistMissing()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Watchlist?)null);

        Assert.Null(await CreateSut().GetMoviesAsync(new MovieFilter
        {
            WatchlistId = WatchlistId,
        }));
        _watchlistService.Verify(
            service => service.RefreshWatchlistAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetMoviesAsync_RefreshesFromImdb_WhenStoredWatchlistHasNoMovies()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist() with { Movies = [] });
        _watchlistService
            .Setup(service => service.RefreshWatchlistAsync(
                WatchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist());

        var movies = await CreateSut().GetMoviesAsync(new MovieFilter
        {
            WatchlistId = WatchlistId,
        });

        Assert.NotNull(movies);
        Assert.Equal(3, movies.Count);
        _watchlistService.Verify(
            service => service.RefreshWatchlistAsync(
                WatchlistId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRandomMovieAsync_ReturnsNull_WhenNoMoviesMatch()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist());

        var movie = await CreateSut().GetRandomMovieAsync(new MovieFilter
        {
            WatchlistId = WatchlistId,
            MinRating = 9.9,
        });

        Assert.Null(movie);
    }

    [Fact]
    public async Task GetRandomMovieAsync_ReturnsMovieFromFilteredSet_WhenMatchesExist()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist());

        var movie = await CreateSut().GetRandomMovieAsync(new MovieFilter
        {
            WatchlistId = WatchlistId,
            Genres = ["Action"],
        });

        Assert.NotNull(movie);
        Assert.Contains(movie.Genres, g => g.Equals("Action", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetRandomMovieAsync_PicksAcrossAllWatchlists_WhenWatchlistIdOmitted()
    {
        _repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateWatchlist(),
                CreateWatchlist() with
                {
                    Id = "ls2",
                    Movies =
                    [
                        new Movie
                        {
                            Id = "tt9",
                            Title = "Other",
                            Year = 2020,
                            Genres = ["Drama"],
                            MediaCategory = MediaCategory.Movie,
                        },
                    ],
                },
            ]);

        var movie = await CreateSut().GetRandomMovieAsync(new MovieFilter());

        Assert.NotNull(movie);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(
            r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetMovieAsync_RefreshesFromImdb_WhenStoredWatchlistHasNoMovies()
    {
        _repository
            .Setup(r => r.GetAsync(WatchlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist() with { Movies = [] });
        _watchlistService
            .Setup(service => service.RefreshWatchlistAsync(
                WatchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist());

        var movie = await CreateSut().GetMovieAsync(WatchlistId, "tt2");

        Assert.NotNull(movie);
        Assert.Equal("Inception", movie.Title);
        _watchlistService.Verify(
            service => service.RefreshWatchlistAsync(
                WatchlistId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Watchlist CreateWatchlist() =>
        new()
        {
            Id = WatchlistId,
            Name = "Test",
            Url = "https://www.imdb.com/list/ls1/",
            LastRefreshedAt = DateTimeOffset.UtcNow,
            Movies =
            [
                new Movie
                {
                    Id = "tt1",
                    Title = "The Matrix",
                    Year = 1999,
                    Rating = 8.7,
                    Genres = ["Action", "Sci-Fi"],
                    MediaCategory = MediaCategory.Movie,
                },
                new Movie
                {
                    Id = "tt2",
                    Title = "Inception",
                    Year = 2010,
                    Rating = 8.8,
                    Genres = ["Action", "Sci-Fi"],
                    MediaCategory = MediaCategory.Movie,
                },
                new Movie
                {
                    Id = "tt3",
                    Title = "Amelie",
                    Year = 2001,
                    Rating = 8.3,
                    Genres = ["Comedy", "Romance"],
                    MediaCategory = MediaCategory.Movie,
                },
            ],
        };
}
