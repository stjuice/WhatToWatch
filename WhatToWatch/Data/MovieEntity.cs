namespace WhatToWatch.Data;

public class MovieEntity
{
    public required string WatchlistId { get; set; }

    public required string Id { get; set; }

    public required string Title { get; set; }

    public int? Year { get; set; }

    public string? PosterUrl { get; set; }

    public double? Rating { get; set; }

    public List<string> Genres { get; set; } = [];

    public WatchlistEntity? Watchlist { get; set; }
}
