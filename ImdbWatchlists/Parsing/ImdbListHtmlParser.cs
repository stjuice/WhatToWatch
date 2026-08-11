using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ImdbWatchlists.Models;

namespace ImdbWatchlists.Parsing;

public static partial class ImdbListHtmlParser
{
    [GeneratedRegex(
        """<script[^>]*id="__NEXT_DATA__"[^>]*>(?<json>.*?)</script>""",
        RegexOptions.Singleline)]
    private static partial Regex NextDataRegex();

    [GeneratedRegex(
        """<meta[^>]*property="og:title"[^>]*content="(?<title>[^"]*)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OgTitleRegex();

    public static Watchlist Parse(string html, string url) => ParsePage(html, url).Watchlist;

    public static ImdbListPage ParsePage(string html, string url)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ImdbWatchlistException(
                $"IMDb returned an empty page for '{url}'. The request was likely blocked.");
        }

        var nextData = NextDataRegex().Match(html);
        if (!nextData.Success)
        {
            throw new ImdbWatchlistException(
                $"Could not find embedded list data in the page for '{url}'. " +
                "IMDb markup may have changed, or the response was a bot challenge.");
        }

        using var document = ParseJson(nextData.Groups["json"].Value, url);

        var movies = new List<Movie>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        CollectMovies(document.RootElement, movies, seenIds);

        return new ImdbListPage
        {
            Watchlist = new Watchlist
            {
                Id = ImdbListUrl.ExtractId(url),
                Name = ExtractName(document.RootElement, html, url),
                Url = url,
                LastRefreshedAt = DateTimeOffset.UtcNow,
                Movies = movies,
            },
            HasNextPage = HasNextPage(document.RootElement),
        };
    }

    private static bool HasNextPage(JsonElement root)
    {
        var search = FindTitleListItemSearch(root);

        return search is not null &&
            search.Value.TryGetProperty("pageInfo", out var pageInfo) &&
            pageInfo.ValueKind == JsonValueKind.Object &&
            pageInfo.TryGetProperty("hasNextPage", out var hasNextPage) &&
            hasNextPage.ValueKind == JsonValueKind.True;
    }

    private static JsonElement? FindTitleListItemSearch(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("titleListItemSearch", out var search) &&
                search.ValueKind == JsonValueKind.Object)
            {
                return search;
            }

            foreach (var property in element.EnumerateObject())
            {
                var found = FindTitleListItemSearch(property.Value);
                if (found is not null)
                {
                    return found;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindTitleListItemSearch(item);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static JsonDocument ParseJson(string json, string url)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new ImdbWatchlistException(
                $"Embedded list data for '{url}' was not valid JSON.", exception);
        }
    }

    private static void CollectMovies(JsonElement element, List<Movie> movies, HashSet<string> seenIds)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryReadMovie(element, out var movie) && seenIds.Add(movie.Id))
                {
                    movies.Add(movie);
                }

                foreach (var property in element.EnumerateObject())
                {
                    CollectMovies(property.Value, movies, seenIds);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectMovies(item, movies, seenIds);
                }

                break;
        }
    }

    private static bool TryReadMovie(JsonElement element, out Movie movie)
    {
        movie = default!;

        if (!element.TryGetProperty("id", out var idElement) ||
            idElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var id = idElement.GetString();
        if (id is null || !id.StartsWith("tt", StringComparison.Ordinal))
        {
            return false;
        }

        var title = ReadString(element, "titleText", "text");
        if (title is null)
        {
            return false;
        }

        movie = new Movie
        {
            Id = id,
            Title = WebUtility.HtmlDecode(title),
            Year = ReadInt(element, "releaseYear", "year"),
            PosterUrl = ReadString(element, "primaryImage", "url"),
            Rating = ReadDouble(element, "ratingsSummary", "aggregateRating"),
            Genres = ReadGenres(element),
        };

        return true;
    }

    private static IReadOnlyCollection<string> ReadGenres(JsonElement element)
    {
        if (!element.TryGetProperty("titleGenres", out var titleGenres) ||
            titleGenres.ValueKind != JsonValueKind.Object ||
            !titleGenres.TryGetProperty("genres", out var genres) ||
            genres.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<string>();
        foreach (var entry in genres.EnumerateArray())
        {
            var genre = ReadString(entry, "genre", "text");
            if (genre is not null)
            {
                result.Add(genre);
            }
        }

        return result;
    }

    private static string ExtractName(JsonElement root, string html, string url)
    {
        var name = FindListName(root);
        if (name is not null)
        {
            return name;
        }

        var ogTitle = OgTitleRegex().Match(html);
        return ogTitle.Success
            ? WebUtility.HtmlDecode(ogTitle.Groups["title"].Value)
            : ImdbListUrl.ExtractId(url);
    }

    private static string? FindListName(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in (string[])["list", "predefinedList"])
            {
                if (element.TryGetProperty(key, out var list) &&
                    list.ValueKind == JsonValueKind.Object)
                {
                    var name = ReadString(list, "name", "originalText")
                        ?? ReadString(list, "nameText", "text");

                    if (name is not null)
                    {
                        return WebUtility.HtmlDecode(name);
                    }
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                var name = FindListName(property.Value);
                if (name is not null)
                {
                    return name;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var name = FindListName(item);
                if (name is not null)
                {
                    return name;
                }
            }
        }

        return null;
    }

    private static string? ReadString(JsonElement element, string objectName, string propertyName)
    {
        if (element.TryGetProperty(objectName, out var child) &&
            child.ValueKind == JsonValueKind.Object &&
            child.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static int? ReadInt(JsonElement element, string objectName, string propertyName)
    {
        if (element.TryGetProperty(objectName, out var child) &&
            child.ValueKind == JsonValueKind.Object &&
            child.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var result))
        {
            return result;
        }

        return null;
    }

    private static double? ReadDouble(JsonElement element, string objectName, string propertyName)
    {
        if (element.TryGetProperty(objectName, out var child) &&
            child.ValueKind == JsonValueKind.Object &&
            child.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetDouble(out var result))
        {
            return result;
        }

        return null;
    }
}
