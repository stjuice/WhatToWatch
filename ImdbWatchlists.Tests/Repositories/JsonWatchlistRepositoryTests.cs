using ImdbWatchlists.Models;
using ImdbWatchlists.Options;
using ImdbWatchlists.Repositories;

namespace ImdbWatchlists.Tests.Repositories;

public class JsonWatchlistRepositoryTests : IDisposable
{
    private readonly string _cacheDirectory =
        Path.Combine(Path.GetTempPath(), $"whattowatch-json-cache-{Guid.NewGuid():N}");

    private readonly JsonWatchlistRepository _repository;

    public JsonWatchlistRepositoryTests()
    {
        _repository = new JsonWatchlistRepository(
            Microsoft.Extensions.Options.Options.Create(new ImdbWatchlistsOptions
            {
                CacheDirectory = _cacheDirectory,
            }));
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        Assert.Null(await _repository.GetAsync("missing"));
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_RoundTripsWatchlist()
    {
        var watchlist = CreateWatchlist("ls055592025", "Favourites");

        await _repository.SaveAsync(watchlist);

        var loaded = await _repository.GetAsync(watchlist.Id);

        Assert.NotNull(loaded);
        Assert.Equal(watchlist.Id, loaded.Id);
        Assert.Equal(watchlist.Name, loaded.Name);
        Assert.Equal(watchlist.Url, loaded.Url);
        Assert.Equal(watchlist.Movies.Count, loaded.Movies.Count);
        Assert.Equal("The Matrix", loaded.Movies.First().Title);
        Assert.Equal(["Action", "Sci-Fi"], loaded.Movies.First().Genres);
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingWatchlist()
    {
        await _repository.SaveAsync(CreateWatchlist("ls1", "Old"));
        await _repository.SaveAsync(CreateWatchlist("ls1", "New"));

        var loaded = await _repository.GetAsync("ls1");

        Assert.Equal("New", loaded!.Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPersistedWatchlists()
    {
        await _repository.SaveAsync(CreateWatchlist("ls1", "One"));
        await _repository.SaveAsync(CreateWatchlist("p.abc", "Two"));

        var all = await _repository.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, item => item.Id == "ls1");
        Assert.Contains(all, item => item.Id == "p.abc");
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, recursive: true);
        }
    }

    private static Watchlist CreateWatchlist(string id, string name) =>
        new()
        {
            Id = id,
            Name = name,
            Url = $"https://www.imdb.com/list/{id}/",
            LastRefreshedAt = DateTimeOffset.Parse("2026-08-11T12:00:00Z"),
            Movies =
            [
                new Movie
                {
                    Id = "tt0133093",
                    Title = "The Matrix",
                    Year = 1999,
                    Rating = 8.7,
                    PosterUrl = "https://example.com/matrix.jpg",
                    Genres = ["Action", "Sci-Fi"],
                },
            ],
        };
}
