using ImdbWatchlists.Parsing;

namespace ImdbWatchlists.Tests.Parsing;

public class ImdbListHtmlParserTests
{
    private const string ListUrl = "https://www.imdb.com/list/ls055592025/";

    [Fact]
    public void Parse_ReadsListNameAndMovies_FromEmbeddedJson()
    {
        var watchlist = ImdbListHtmlParser.Parse(BuildHtml(), ListUrl);

        Assert.Equal("ls055592025", watchlist.Id);
        Assert.Equal("My Favourites", watchlist.Name);
        Assert.Equal(ListUrl, watchlist.Url);
        Assert.Equal(2, watchlist.Movies.Count);
    }

    [Fact]
    public void Parse_MapsTitleFields()
    {
        var watchlist = ImdbListHtmlParser.Parse(BuildHtml(), ListUrl);

        var movie = watchlist.Movies.First();

        Assert.Equal("tt0133093", movie.Id);
        Assert.Equal("The Matrix", movie.Title);
        Assert.Equal(1999, movie.Year);
        Assert.Equal(8.7, movie.Rating);
        Assert.Equal("https://example.com/matrix.jpg", movie.PosterUrl);
        Assert.Equal(["Action", "Sci-Fi"], movie.Genres);
        Assert.Equal(
            "A computer hacker learns from mysterious rebels about the true nature of his reality.",
            movie.Plot);
        Assert.Equal(136, movie.RuntimeMinutes);
        Assert.Equal("Lana Wachowski", movie.Director);
    }

    [Fact]
    public void Parse_SkipsDuplicateTitles()
    {
        var watchlist = ImdbListHtmlParser.Parse(BuildHtml(), ListUrl);

        Assert.Single(watchlist.Movies, movie => movie.Id == "tt0133093");
    }

    [Fact]
    public void Parse_Throws_WhenPageIsEmpty()
    {
        var exception = Assert.Throws<ImdbWatchlistException>(
            () => ImdbListHtmlParser.Parse(string.Empty, ListUrl));

        Assert.Contains("empty page", exception.Message);
    }

    [Fact]
    public void Parse_Throws_WhenEmbeddedDataIsMissing()
    {
        var exception = Assert.Throws<ImdbWatchlistException>(
            () => ImdbListHtmlParser.Parse("<html><body>challenge</body></html>", ListUrl));

        Assert.Contains("embedded list data", exception.Message);
    }

    [Fact]
    public void Parse_ReadsMovie_WhenRatingAndYearAreNull()
    {
        const string html =
            """
            <html><body>
            <script id="__NEXT_DATA__" type="application/json">
            {
              "props": { "pageProps": { "mainColumnData": { "list": {
                "name": { "originalText": "Unrated" },
                "titleListItemSearch": { "edges": [
                  { "listItem": {
                      "id": "tt9999999",
                      "titleText": { "text": "Unreleased Film" },
                      "releaseYear": { "year": null },
                      "ratingsSummary": { "aggregateRating": null }
                  } }
                ] }
              } } } }
            }
            </script>
            </body></html>
            """;

        var movie = Assert.Single(ImdbListHtmlParser.Parse(html, ListUrl).Movies);

        Assert.Equal("Unreleased Film", movie.Title);
        Assert.Null(movie.Year);
        Assert.Null(movie.Rating);
        Assert.Null(movie.Plot);
        Assert.Null(movie.RuntimeMinutes);
        Assert.Null(movie.Director);
    }

    [Fact]
    public void Parse_ReadsPlotRuntimeAndDirector_WhenPresent()
    {
        var movie = ImdbListHtmlParser.Parse(BuildHtml(), ListUrl).Movies.First();

        Assert.Equal(
            "A computer hacker learns from mysterious rebels about the true nature of his reality.",
            movie.Plot);
        Assert.Equal(136, movie.RuntimeMinutes);
        Assert.Equal("Lana Wachowski", movie.Director);
    }

    [Fact]
    public void Parse_LeavesPlotRuntimeDirectorNull_WhenMissingFromEmbed()
    {
        var inception = ImdbListHtmlParser.Parse(BuildHtml(), ListUrl).Movies
            .Single(movie => movie.Id == "tt1375666");

        Assert.Equal("Inception", inception.Title);
        Assert.Null(inception.Plot);
        Assert.Null(inception.RuntimeMinutes);
        Assert.Null(inception.Director);
    }

    [Fact]
    public void Parse_ReadsDirector_FromPrincipalCreditsV2()
    {
        const string html =
            """
            <html><body>
            <script id="__NEXT_DATA__" type="application/json">
            {
              "props": { "pageProps": { "mainColumnData": { "list": {
                "name": { "originalText": "Credits V2" },
                "titleListItemSearch": { "edges": [
                  { "listItem": {
                      "id": "tt0068646",
                      "titleText": { "text": "The Godfather" },
                      "principalCreditsV2": [
                        {
                          "grouping": { "text": "Director" },
                          "credits": [
                            { "name": { "nameText": { "text": "Francis Ford Coppola" } } }
                          ]
                        },
                        {
                          "grouping": { "text": "Stars" },
                          "credits": [
                            { "name": { "nameText": { "text": "Marlon Brando" } } }
                          ]
                        }
                      ]
                  } }
                ] }
              } } } }
            }
            </script>
            </body></html>
            """;

        var movie = Assert.Single(ImdbListHtmlParser.Parse(html, ListUrl).Movies);

        Assert.Equal("Francis Ford Coppola", movie.Director);
    }

    private static string BuildHtml() =>
        """
        <html>
        <head><meta property="og:title" content="Ignored when JSON has a name" /></head>
        <body>
        <script id="__NEXT_DATA__" type="application/json">
        {
          "props": {
            "pageProps": {
              "mainColumnData": {
                "list": {
                  "id": "ls055592025",
                  "name": { "originalText": "My Favourites" },
                  "titleListItemSearch": {
                    "edges": [
                      {
                        "listItem": {
                          "id": "tt0133093",
                          "titleText": { "text": "The Matrix" },
                          "releaseYear": { "year": 1999 },
                          "primaryImage": { "url": "https://example.com/matrix.jpg" },
                          "ratingsSummary": { "aggregateRating": 8.7 },
                          "plot": {
                            "plotText": {
                              "plainText": "A computer hacker learns from mysterious rebels about the true nature of his reality."
                            }
                          },
                          "runtime": { "seconds": 8160 },
                          "principalCredits": [
                            {
                              "category": { "id": "director", "text": "Directors" },
                              "credits": [
                                {
                                  "name": {
                                    "nameText": { "text": "Lana Wachowski" }
                                  }
                                },
                                {
                                  "name": {
                                    "nameText": { "text": "Lilly Wachowski" }
                                  }
                                }
                              ]
                            },
                            {
                              "category": { "id": "writer", "text": "Writers" },
                              "credits": [
                                {
                                  "name": {
                                    "nameText": { "text": "Lilly Wachowski" }
                                  }
                                }
                              ]
                            }
                          ],
                          "titleGenres": {
                            "genres": [
                              { "genre": { "text": "Action" } },
                              { "genre": { "text": "Sci-Fi" } }
                            ]
                          }
                        }
                      },
                      {
                        "listItem": {
                          "id": "tt0133093",
                          "titleText": { "text": "The Matrix" }
                        }
                      },
                      {
                        "listItem": {
                          "id": "tt1375666",
                          "titleText": { "text": "Inception" },
                          "releaseYear": { "year": 2010 }
                        }
                      }
                    ]
                  }
                }
              }
            }
          }
        }
        </script>
        </body>
        </html>
        """;
}
