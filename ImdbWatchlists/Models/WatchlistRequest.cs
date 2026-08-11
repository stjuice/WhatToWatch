namespace ImdbWatchlists.Models;

public record WatchlistRequest
{
    public required string Url { get; init; }

    public WatchlistAccess Access { get; init; } = WatchlistAccess.Public;
}
