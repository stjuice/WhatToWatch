namespace WhatToWatch.DTOs;

public record MovieFilterRequest
{
    public string? Query { get; init; }

    public int? YearFrom { get; init; }

    public int? YearTo { get; init; }

    public double? MinRating { get; init; }

    public IReadOnlyCollection<string>? Genres { get; init; }
}
