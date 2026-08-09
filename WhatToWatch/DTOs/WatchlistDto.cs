namespace WhatToWatch.DTOs;

public record WatchlistDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public DateTimeOffset? LastRefreshedAt { get; init; }

    public IReadOnlyCollection<MovieDto> Movies { get; init; } = [];
}
