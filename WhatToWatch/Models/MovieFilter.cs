namespace WhatToWatch.Models;

public record MovieFilter
{
    public string? Query { get; init; }

    public int? YearFrom { get; init; }

    public int? YearTo { get; init; }

    public double? MinRating { get; init; }

    public IReadOnlyCollection<string> Genres { get; init; } = [];
}
