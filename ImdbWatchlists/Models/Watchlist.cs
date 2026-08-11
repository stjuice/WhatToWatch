namespace ImdbWatchlists.Models;

public record Watchlist
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Url { get; init; }

    public DateTimeOffset? LastRefreshedAt { get; init; }

    public IReadOnlyCollection<Movie> Movies { get; init; } = [];
}
