using ImdbWatchlists.Models;

namespace ImdbWatchlists.Parsing;

public sealed record ImdbListPage
{
    public required Watchlist Watchlist { get; init; }

    public required bool HasNextPage { get; init; }
}
