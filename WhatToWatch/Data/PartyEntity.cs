namespace WhatToWatch.Data;

public class PartyEntity
{
    public required string Id { get; set; }

    public required string JoinCode { get; set; }

    public string? WatchlistId { get; set; }

    public List<string> MovieSet { get; set; } = [];

    public PartyStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public string? MatchedMovieId { get; set; }

    public string? MatchedWatchlistId { get; set; }

    public List<PartyPlayerEntity> Players { get; set; } = [];

    public List<PartyLikeEntity> Likes { get; set; } = [];
}
