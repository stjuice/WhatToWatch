using System.ComponentModel.DataAnnotations;

namespace WhatToWatch.DTOs;

public record ImportImdbWatchlistRequest
{
    [Required]
    public required string ListId { get; init; }

    [Required]
    public required string Title { get; init; }

    [Required]
    [MinLength(1)]
    public required IReadOnlyCollection<ImportImdbMovieRequest> Movies { get; init; }
}

public record ImportImdbMovieRequest
{
    [Required]
    public required string ImdbId { get; init; }

    [Required]
    public required string Title { get; init; }

    public int? Year { get; init; }

    public string? ImageUrl { get; init; }

    public double? Rating { get; init; }

    public string? Plot { get; init; }

    public int? RuntimeMinutes { get; init; }

    public string? Director { get; init; }

    public IReadOnlyCollection<string>? Genres { get; init; }
}
