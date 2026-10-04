namespace WhatToWatch.DTOs;

public record SpookieTicketDto
{
    public required string Key { get; init; }

    public bool IsBonus { get; init; }

    public bool IsCurrent { get; init; }

    public required MovieDto Movie { get; init; }
}
