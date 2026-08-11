namespace ImdbWatchlists.Models;

public record Movie
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public int? Year { get; init; }

    public string? PosterUrl { get; init; }

    public double? Rating { get; init; }

    public IReadOnlyCollection<string> Genres { get; init; } = [];
}
