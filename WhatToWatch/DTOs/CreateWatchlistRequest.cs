using System.ComponentModel.DataAnnotations;

namespace WhatToWatch.DTOs;

public record CreateWatchlistRequest
{
    [Required]
    public required string Url { get; init; }
}
