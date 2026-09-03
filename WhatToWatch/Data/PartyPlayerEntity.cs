namespace WhatToWatch.Data;

public class PartyPlayerEntity
{
    public required string Id { get; set; }

    public required string PartyId { get; set; }

    public required string PlayerToken { get; set; }

    public int Slot { get; set; }

    public List<string> MovieOrder { get; set; } = [];

    public int CurrentIndex { get; set; }

    public DateTimeOffset JoinedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public PartyEntity? Party { get; set; }
}
