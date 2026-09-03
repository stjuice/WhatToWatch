using ImdbWatchlists.Models;
using ImdbWatchlists.Parsing;

namespace ImdbWatchlists.Extraction;

public static class ImdbExtractedPageMapper
{
    public static ImdbListPage ToListPage(
        ExtractedWatchlistPage page,
        string listUrl,
        string pageUrl)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(listUrl);

        return new ImdbListPage
        {
            Watchlist = new Watchlist
            {
                Id = ImdbListUrl.ExtractId(listUrl),
                Name = page.Title.Trim(),
                Url = listUrl,
                LastRefreshedAt = DateTimeOffset.UtcNow,
                Movies = DeduplicateMovies(page.Movies),
            },
            HasNextPage = page.HasNextPage,
        };
    }

    public static Watchlist ToImportWatchlist(
        ExtractedWatchlistPage page,
        Watchlist? existing = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(page.ListId);
        ArgumentException.ThrowIfNullOrWhiteSpace(page.Title);

        if (page.Movies is null || page.Movies.Count == 0)
            throw new ArgumentException("At least one movie is required.", nameof(page));

        var listId = page.ListId.Trim();

        return new Watchlist
        {
            Id = listId,
            Name = page.Title.Trim(),
            Url = ResolveImportUrl(page.Url, listId, existing?.Url),
            LastRefreshedAt = DateTimeOffset.UtcNow,
            Movies = DeduplicateMovies(page.Movies),
        };
    }

    public static IReadOnlyCollection<Movie> DeduplicateMovies(
        IReadOnlyCollection<ExtractedMoviePage> movies)
    {
        ArgumentNullException.ThrowIfNull(movies);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<Movie>(movies.Count);

        foreach (var movie in movies)
        {
            if (string.IsNullOrWhiteSpace(movie.ImdbId) || string.IsNullOrWhiteSpace(movie.Title))
                continue;

            var id = movie.ImdbId.Trim();
            if (!seen.Add(id))
                continue;

            result.Add(ToMovie(movie));
        }

        if (result.Count == 0)
            throw new ArgumentException("At least one valid movie is required.");

        return result;
    }

    public static Movie ToMovie(ExtractedMoviePage movie)
    {
        ArgumentNullException.ThrowIfNull(movie);

        return new Movie
        {
            Id = movie.ImdbId.Trim(),
            Title = movie.Title.Trim(),
            Year = movie.Year,
            PosterUrl = string.IsNullOrWhiteSpace(movie.ImageUrl) ? null : movie.ImageUrl.Trim(),
            Rating = movie.Rating,
            Plot = string.IsNullOrWhiteSpace(movie.Plot) ? null : movie.Plot.Trim(),
            RuntimeMinutes = movie.RuntimeMinutes,
            Director = string.IsNullOrWhiteSpace(movie.Director) ? null : movie.Director.Trim(),
            Genres = NormalizeGenres(movie.Genres),
            MediaCategory = ImdbTitleType.FromId(movie.TitleType),
        };
    }

    private static string ResolveImportUrl(string? pageUrl, string listId, string? existingUrl)
    {
        if (!string.IsNullOrWhiteSpace(pageUrl))
            return pageUrl.Trim();

        if (!string.IsNullOrWhiteSpace(existingUrl))
            return existingUrl;

        return BuildDefaultListUrl(listId);
    }

    private static string BuildDefaultListUrl(string listId)
    {
        if (listId.StartsWith("ur", StringComparison.OrdinalIgnoreCase))
            return $"https://www.imdb.com/user/{listId}/watchlist/";

        return $"https://www.imdb.com/list/{listId}/";
    }

    private static IReadOnlyCollection<string> NormalizeGenres(IReadOnlyCollection<string>? genres)
    {
        if (genres is null || genres.Count == 0)
            return [];

        return [.. genres
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Select(genre => genre.Trim())];
    }
}
