namespace WhatToWatch.Data;

public class PartyLikeEntity
{
    public required string PartyId { get; set; }

    public required string PlayerId { get; set; }

    public required string MovieId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public PartyEntity? Party { get; set; }
}
