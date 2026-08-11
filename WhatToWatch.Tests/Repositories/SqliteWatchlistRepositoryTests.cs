using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatToWatch.Data;
using WhatToWatch.Models;
using WhatToWatch.Repositories;

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
        Assert.Equal("The Matrix", loaded.Movies.First().Title);
        Assert.Equal(["Action", "Sci-Fi"], loaded.Movies.First().Genres);
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

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Watchlist CreateWatchlist(
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
                new Movie
                {
                    Id = "tt0133093",
                    Title = movieTitle,
                    Year = 1999,
                    PosterUrl = "https://example.com/poster.jpg",
                    Rating = 8.7,
                    Genres = ["Action", "Sci-Fi"],
                },
            ],
        };
}
