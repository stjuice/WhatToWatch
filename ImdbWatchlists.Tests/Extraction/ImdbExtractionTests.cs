using ImdbWatchlists.Extraction;
using ImdbWatchlists.Models;

namespace ImdbWatchlists.Tests.Extraction;

public class PlaywrightJsImdbPageExtractorTests
{
    private const string PageUrl = "https://www.imdb.com/list/ls055592025/";

    [Fact]
    public void ParseExtractedJson_ReadsMovies()
    {
        const string json =
            """
            {
              "listId": "ls055592025",
              "title": "My Favourites",
              "movies": [
                {
                  "imdbId": "tt0133093",
                  "title": "The Matrix",
                  "year": 1999,
                  "genres": ["Action"]
                }
              ],
              "hasNextPage": false,
              "nextPageUrl": null
            }
            """;

        var page = PlaywrightJsImdbPageExtractor.ParseExtractedJson(json, PageUrl);

        Assert.Equal("ls055592025", page.ListId);
        Assert.Equal("My Favourites", page.Title);
        Assert.False(page.HasNextPage);
        Assert.Equal("tt0133093", Assert.Single(page.Movies).ImdbId);
    }

    [Fact]
    public void ParseExtractedJson_Throws_WhenJsonIsNull()
    {
        var exception = Assert.Throws<ImdbWatchlistException>(() =>
            PlaywrightJsImdbPageExtractor.ParseExtractedJson("null", PageUrl));

        Assert.Contains("Could not find embedded list data", exception.Message);
    }

    [Fact]
    public void ParseExtractedJson_Throws_WhenMoviesAreMissing()
    {
        const string json =
            """
            {
              "listId": "ls055592025",
              "title": "Empty",
              "movies": [],
              "hasNextPage": false
            }
            """;

        var exception = Assert.Throws<ImdbWatchlistException>(() =>
            PlaywrightJsImdbPageExtractor.ParseExtractedJson(json, PageUrl));

        Assert.Contains("Could not find embedded list data", exception.Message);
    }

    [Fact]
    public void EmbeddedExtractScript_IsPresent()
    {
        var assembly = typeof(PlaywrightJsImdbPageExtractor).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            "ImdbWatchlists.Scripts.extractWatchlist.js");

        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        var script = reader.ReadToEnd();

        Assert.Contains("extractWatchlistFromNextData", script);
        Assert.Contains("__NEXT_DATA__", script);
    }
}

public class ImdbExtractedPageMapperTests
{
    private const string ListUrl = "https://www.imdb.com/list/ls055592025/";

    [Fact]
    public void ToListPage_MapsMoviesAndPagination()
    {
        var extracted = new ExtractedWatchlistPage
        {
            ListId = "ls055592025",
            Title = "My Favourites",
            HasNextPage = true,
            NextPageUrl = $"{ListUrl}?page=2",
            Movies =
            [
                new ExtractedMoviePage
                {
                    ImdbId = "tt0133093",
                    Title = "The Matrix",
                    Year = 1999,
                    ImageUrl = "https://example.com/matrix.jpg",
                    Rating = 8.7,
                    Plot = "Plot",
                    RuntimeMinutes = 136,
                    Director = "Lana Wachowski",
                    Genres = ["Action", "Sci-Fi"],
                    TitleType = "movie",
                },
            ],
        };

        var listPage = ImdbExtractedPageMapper.ToListPage(extracted, ListUrl, ListUrl);

        Assert.True(listPage.HasNextPage);
        Assert.Equal("ls055592025", listPage.Watchlist.Id);
        Assert.Equal("My Favourites", listPage.Watchlist.Name);

        var movie = Assert.Single(listPage.Watchlist.Movies);
        Assert.Equal("tt0133093", movie.Id);
        Assert.Equal(MediaCategory.Movie, movie.MediaCategory);
        Assert.Equal(["Action", "Sci-Fi"], movie.Genres);
    }

    [Theory]
    [InlineData("tvSeries", MediaCategory.TvShow)]
    [InlineData("movie", MediaCategory.Movie)]
    [InlineData(null, MediaCategory.Unknown)]
    public void ToListPage_MapsTitleType(string? titleType, MediaCategory expected)
    {
        var extracted = new ExtractedWatchlistPage
        {
            ListId = "ls055592025",
            Title = "Typed",
            HasNextPage = false,
            Movies =
            [
                new ExtractedMoviePage
                {
                    ImdbId = "tt0133093",
                    Title = "The Matrix",
                    TitleType = titleType,
                },
            ],
        };

        var movie = Assert.Single(
            ImdbExtractedPageMapper.ToListPage(extracted, ListUrl, ListUrl).Watchlist.Movies);

        Assert.Equal(expected, movie.MediaCategory);
    }

    [Fact]
    public void ToImportWatchlist_DeduplicatesMoviesAndResolvesUrl()
    {
        var extracted = new ExtractedWatchlistPage
        {
            ListId = "ls055592025",
            Title = "Sci-Fi",
            Url = ListUrl,
            HasNextPage = false,
            Movies =
            [
                new ExtractedMoviePage
                {
                    ImdbId = "tt0133093",
                    Title = "The Matrix",
                    TitleType = "movie",
                },
                new ExtractedMoviePage
                {
                    ImdbId = "tt0133093",
                    Title = "Duplicate",
                    TitleType = "movie",
                },
            ],
        };

        var watchlist = ImdbExtractedPageMapper.ToImportWatchlist(extracted);

        Assert.Equal("ls055592025", watchlist.Id);
        Assert.Equal(ListUrl, watchlist.Url);
        Assert.Equal("tt0133093", Assert.Single(watchlist.Movies).Id);
    }
}
