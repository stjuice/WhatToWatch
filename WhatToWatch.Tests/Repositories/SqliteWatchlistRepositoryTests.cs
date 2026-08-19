using ImdbWatchlists.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatToWatch.Data;
using WhatToWatch.Repositories;
using MovieModel = WhatToWatch.Models.Movie;
using WatchlistModel = WhatToWatch.Models.Watchlist;

namespace WhatToWatch.Tests.Repositories;

public class SqliteWatchlistRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WhatToWatchDbContext _db;
    private readonly SqliteWatchlistRepository _repository;

    public SqliteWatchlistRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WhatToWatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new WhatToWatchDbContext(options);
        _db.Database.EnsureCreated();
        _repository = new SqliteWatchlistRepository(_db);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_RoundTripsWatchlist()
    {
        var watchlist = CreateWatchlist("ls1", "Favourites");

        await _repository.SaveAsync(watchlist);

        var loaded = await _repository.GetAsync(watchlist.Id);

        Assert.NotNull(loaded);
        Assert.Equal(watchlist.Id, loaded.Id);
        Assert.Equal(watchlist.Name, loaded.Name);
        Assert.Equal(watchlist.Url, loaded.Url);
        Assert.Equal(watchlist.LastRefreshedAt, loaded.LastRefreshedAt);
        Assert.Single(loaded.Movies);

        var movie = loaded.Movies.First();
        Assert.Equal("The Matrix", movie.Title);
        Assert.Equal(["Action", "Sci-Fi"], movie.Genres);
        Assert.Equal("A computer hacker learns about the true nature of reality.", movie.Plot);
        Assert.Equal(136, movie.RuntimeMinutes);
        Assert.Equal("Lana Wachowski", movie.Director);
        Assert.Equal(MediaCategory.Movie, movie.MediaCategory);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_RoundTripsMediaCategory()
    {
        var watchlist = CreateWatchlist("ls1", "Mixed") with
        {
            Movies =
            [
                new MovieModel
                {
                    Id = "tt1",
                    Title = "Film",
                    Genres = ["Drama"],
                    MediaCategory = MediaCategory.Movie,
                },
                new MovieModel
                {
                    Id = "tt2",
                    Title = "Series",
                    Genres = ["Drama"],
                    MediaCategory = MediaCategory.TvShow,
                },
                new MovieModel
                {
                    Id = "tt3",
                    Title = "Unknown",
                    Genres = ["Drama"],
                    MediaCategory = MediaCategory.Unknown,
                },
            ],
        };

        await _repository.SaveAsync(watchlist);
        var loaded = await _repository.GetAsync("ls1");

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.Movies.Count);
        Assert.Equal(
            MediaCategory.Movie,
            loaded.Movies.Single(movie => movie.Id == "tt1").MediaCategory);
        Assert.Equal(
            MediaCategory.TvShow,
            loaded.Movies.Single(movie => movie.Id == "tt2").MediaCategory);
        Assert.Equal(
            MediaCategory.Unknown,
            loaded.Movies.Single(movie => movie.Id == "tt3").MediaCategory);
    }

    [Fact]
    public async Task EnsureMediaCategoryColumnAsync_IsIdempotent_AndDefaultsExistingRows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var create = connection.CreateCommand())
        {
            create.CommandText =
                """
                CREATE TABLE "Watchlists" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Watchlists" PRIMARY KEY,
                    "Name" TEXT NOT NULL,
                    "Url" TEXT NULL,
                    "LastRefreshedAt" TEXT NULL
                );
                CREATE TABLE "Movies" (
                    "WatchlistId" TEXT NOT NULL,
                    "Id" TEXT NOT NULL,
                    "Title" TEXT NOT NULL,
                    "Year" INTEGER NULL,
                    "PosterUrl" TEXT NULL,
                    "Rating" REAL NULL,
                    "Plot" TEXT NULL,
                    "RuntimeMinutes" INTEGER NULL,
                    "Director" TEXT NULL,
                    "GenresJson" TEXT NOT NULL,
                    CONSTRAINT "PK_Movies" PRIMARY KEY ("WatchlistId", "Id")
                );
                INSERT INTO "Watchlists" ("Id", "Name") VALUES ('ls1', 'Legacy');
                INSERT INTO "Movies" ("WatchlistId", "Id", "Title", "GenresJson")
                VALUES ('ls1', 'tt1', 'Legacy Title', '[]');
                """;
            await create.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<WhatToWatchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new WhatToWatchDbContext(options);
        await SqliteSchemaUpgrades.EnsureMediaCategoryColumnAsync(db);
        await SqliteSchemaUpgrades.EnsureMediaCategoryColumnAsync(db);

        var repository = new SqliteWatchlistRepository(db);
        var loaded = await repository.GetAsync("ls1");

        Assert.NotNull(loaded);
        Assert.Equal(MediaCategory.Unknown, loaded.Movies.Single().MediaCategory);
    }

    [Fact]
    public async Task SaveAsync_ReplacesMoviesOnOverwrite()
    {
        await _repository.SaveAsync(CreateWatchlist("ls1", "Old", movieTitle: "Old Movie"));
        await _repository.SaveAsync(CreateWatchlist("ls1", "New", movieTitle: "New Movie"));

        var loaded = await _repository.GetAsync("ls1");

        Assert.Equal("New", loaded!.Name);
        Assert.Single(loaded.Movies);
        Assert.Equal("New Movie", loaded.Movies.First().Title);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPersistedWatchlists()
    {
        await _repository.SaveAsync(CreateWatchlist("ls1", "Alpha"));
        await _repository.SaveAsync(CreateWatchlist("ls2", "Beta"));

        var all = await _repository.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, item => item.Id == "ls1");
        Assert.Contains(all, item => item.Id == "ls2");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        Assert.Null(await _repository.GetAsync("missing"));
    }

    [Fact]
    public async Task GetByUrlAsync_ReturnsWatchlistWithMovies_WhenUrlMatches()
    {
        await _repository.SaveAsync(CreateWatchlist("ls1", "Favourites"));

        var loaded = await _repository.GetByUrlAsync("https://www.imdb.com/list/ls1/");

        Assert.NotNull(loaded);
        Assert.Equal("ls1", loaded.Id);
        Assert.Single(loaded.Movies);
    }

    [Theory]
    [InlineData("https://www.imdb.com/list/ls1")]
    [InlineData("https://WWW.IMDB.COM/list/ls1/")]
    [InlineData("  https://www.imdb.com/list/ls1/  ")]
    public async Task GetByUrlAsync_MatchesRegardlessOfCasingTrailingSlashOrWhitespace(string url)
    {
        await _repository.SaveAsync(CreateWatchlist("ls1", "Favourites"));

        var loaded = await _repository.GetByUrlAsync(url);

        Assert.NotNull(loaded);
        Assert.Equal("ls1", loaded.Id);
    }

    [Fact]
    public async Task GetByUrlAsync_ReturnsNull_WhenUrlIsNotStored()
    {
        await _repository.SaveAsync(CreateWatchlist("ls1", "Favourites"));

        Assert.Null(await _repository.GetByUrlAsync("https://www.imdb.com/list/ls999/"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesWatchlistAndMovies()
    {
        await _repository.SaveAsync(CreateWatchlist("ls1", "Favourites"));

        Assert.True(await _repository.DeleteAsync("ls1"));
        Assert.Null(await _repository.GetAsync("ls1"));
        Assert.False(await _repository.DeleteAsync("ls1"));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static WatchlistModel CreateWatchlist(
        string id,
        string name,
        string movieTitle = "The Matrix") =>
        new()
        {
            Id = id,
            Name = name,
            Url = $"https://www.imdb.com/list/{id}/",
            LastRefreshedAt = DateTimeOffset.Parse("2024-01-15T12:00:00Z"),
            Movies =
            [
                new MovieModel
                {
                    Id = "tt0133093",
                    Title = movieTitle,
                    Year = 1999,
                    PosterUrl = "https://example.com/poster.jpg",
                    Rating = 8.7,
                    Plot = "A computer hacker learns about the true nature of reality.",
                    RuntimeMinutes = 136,
                    Director = "Lana Wachowski",
                    Genres = ["Action", "Sci-Fi"],
                    MediaCategory = MediaCategory.Movie,
                },
            ],
        };
}
