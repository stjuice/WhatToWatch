namespace WhatToWatch.DTOs;

public record SpookieMovieDto
{
    public required MovieDto Movie { get; init; }

    public string? Link { get; init; }
}

public record SpookieTicketDto
{
    public required string Key { get; init; }

    public bool IsBonus { get; init; }

    public bool IsCurrent { get; init; }

    public required MovieDto Movie { get; init; }
}
