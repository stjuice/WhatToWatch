namespace WhatToWatch.DTOs;

public class MovieFilterRequest
{
    public string? Query { get; set; }

    public int? YearFrom { get; set; }

    public int? YearTo { get; set; }

    public double? MinRating { get; set; }

    public IReadOnlyCollection<string>? Genres { get; set; }
}
