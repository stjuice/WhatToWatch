namespace ImdbWatchlists.Extraction;

public sealed record ExtractedMoviePage
{
    public required string ImdbId { get; init; }

    public required string Title { get; init; }

    public int? Year { get; init; }

    public string? ImageUrl { get; init; }

    public double? Rating { get; init; }

    public string? Plot { get; init; }

    public int? RuntimeMinutes { get; init; }

    public string? Director { get; init; }

    public IReadOnlyCollection<string> Genres { get; init; } = [];

    public string? TitleType { get; init; }
}

public sealed record ExtractedWatchlistPage
{
    public required string ListId { get; init; }

    public required string Title { get; init; }

    public required IReadOnlyCollection<ExtractedMoviePage> Movies { get; init; }

    public required bool HasNextPage { get; init; }

    public string? NextPageUrl { get; init; }

    /// <summary>Optional source URL (native import). Ignored during server-side page extraction.</summary>
    public string? Url { get; init; }
}
