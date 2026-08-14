using System.ComponentModel.DataAnnotations;

namespace WhatToWatch.DTOs;

public record UpdateWatchlistRequest
{
    [Required]
    [MinLength(1)]
    public required string Name { get; init; }
}
