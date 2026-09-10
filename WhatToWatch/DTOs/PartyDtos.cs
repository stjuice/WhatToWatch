namespace WhatToWatch.DTOs;

public sealed record CreatePartyRequest
{
    public string? WatchlistId { get; init; }
    public string? JoinCode { get; init; }
}

public sealed record JoinPartyRequest
{
    public required string JoinCode { get; init; }
}

public sealed record PartyVoteRequest
{
    public required string MovieId { get; init; }
    public bool Liked { get; init; }
}

public sealed record PartyProgressDto
{
    public int CurrentIndex { get; init; }
    public int TotalMovies { get; init; }
    public bool IsExhausted { get; init; }
}

public record PartyStateDto
{
    public required string PartyId { get; init; }
    public required string JoinCode { get; init; }
    public required string Status { get; init; }
    public int PlayerCount { get; init; }
    public bool OpponentPresent { get; init; }
    public bool OpponentOnline { get; init; }
    public required PartyProgressDto Progress { get; init; }
    public MovieDto? CurrentMovie { get; init; }
    public MovieDto? MatchedMovie { get; init; }
}

public sealed record PartySessionDto : PartyStateDto
{
    public required string PlayerToken { get; init; }
}

public sealed record PartyPreviewDto
{
    public required string PartyId { get; init; }
    public required string JoinCode { get; init; }
    public required string Status { get; init; }
    public int PlayerCount { get; init; }
    public bool IsFull { get; init; }
}

public sealed record SuggestedCodeDto
{
    public required string JoinCode { get; init; }
}

public sealed record PartyErrorDto
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}
