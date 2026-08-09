namespace WhatToWatch.DTOs;

public class WatchlistDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset? LastRefreshedAt { get; set; }

    public IReadOnlyCollection<MovieDto> Movies { get; set; } = Array.Empty<MovieDto>();
}
