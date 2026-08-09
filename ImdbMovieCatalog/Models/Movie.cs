namespace ImdbMovieCatalog.Models;

public class Movie
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int? Year { get; set; }

    public string? PosterUrl { get; set; }

    public double? Rating { get; set; }

    public IReadOnlyCollection<string> Genres { get; set; } = Array.Empty<string>();
}
