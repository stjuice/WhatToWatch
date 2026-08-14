namespace WhatToWatch.DTOs;

public record MovieFilterRequest
{
    /// <summary>
    /// When omitted or empty, movie queries run across every stored watchlist.
    /// </summary>
    public string? WatchlistId { get; init; }

    public string? Query { get; init; }

    public int? YearFrom { get; init; }

    public int? YearTo { get; init; }

    public double? MinRating { get; init; }

    public IReadOnlyCollection<string>? Genres { get; init; }
}
