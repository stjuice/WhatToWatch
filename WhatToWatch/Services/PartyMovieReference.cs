using System.Text.Json;

namespace WhatToWatch.Services;

public sealed record PartyMovieKey(string WatchlistId, string MovieId);

public static class PartyMovieReference
{
    public static string Encode(string watchlistId, string movieId) =>
        JsonSerializer.Serialize(new PartyMovieKey(watchlistId, movieId));

    public static PartyMovieKey Decode(string value) =>
        JsonSerializer.Deserialize<PartyMovieKey>(value)
        ?? throw new InvalidDataException("Invalid party movie reference.");
}
