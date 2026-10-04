using ImdbWatchlists.Models;

namespace WhatToWatch.Services;

public interface ISpookieNightService
{
    Task<IReadOnlyList<SpookieTicket>> GetUnlockedTicketsAsync(
        CancellationToken cancellationToken = default);

    Task<SpookieTicket?> GetTicketAsync(
        string key,
        CancellationToken cancellationToken = default);
}

public sealed record SpookieTicket(string Key, bool IsBonus, bool IsCurrent, Movie Movie);
