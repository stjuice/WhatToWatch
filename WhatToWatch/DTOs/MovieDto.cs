namespace WhatToWatch.DTOs;

public record MovieDto
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public int? Year { get; init; }

    public string? PosterUrl { get; init; }

    public double? Rating { get; init; }

    public string? Plot { get; init; }

    public int? RuntimeMinutes { get; init; }

    public string? Director { get; init; }

    public IReadOnlyCollection<string> Genres { get; init; } = [];
}
