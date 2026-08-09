namespace ImdbMovieCatalog.Models;

public class Watchlist
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset? LastRefreshedAt { get; set; }

    public IReadOnlyCollection<Movie> Movies { get; set; } = Array.Empty<Movie>();
}
