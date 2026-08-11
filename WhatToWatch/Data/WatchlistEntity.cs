namespace WhatToWatch.Data;

public class WatchlistEntity
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public string? Url { get; set; }

    public DateTimeOffset? LastRefreshedAt { get; set; }

    public List<MovieEntity> Movies { get; set; } = [];
}
