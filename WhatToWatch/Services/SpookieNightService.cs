using ImdbWatchlists.Models;
using Microsoft.Extensions.Options;
using WhatToWatch.Options;

namespace WhatToWatch.Services;

public sealed class SpookieNightService(
    IMovieService movieService,
    TimeProvider timeProvider,
    IOptions<SpookieNightOptions> options,
    ILogger<SpookieNightService> logger) : ISpookieNightService
{
    private readonly SpookieNightOptions _options = options.Value;

    public async Task<IReadOnlyList<SpookieTicket>> GetUnlockedTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        var unlocked = GetUnlockedTicketOptions();
        var current = FindCurrent(unlocked);
        var tickets = new List<SpookieTicket>(unlocked.Count);

        foreach (var ticket in unlocked)
        {
            var movie = await FindMovieInListAsync(ticket, cancellationToken).ConfigureAwait(false);
            if (movie is not null)
                tickets.Add(ToTicket(ticket, movie, current));
        }

        return tickets;
    }

    public async Task<SpookieTicket?> GetTicketAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var unlocked = GetUnlockedTicketOptions();
        var ticket = unlocked.FirstOrDefault(candidate =>
            candidate.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (ticket is null)
            return null;

        var movie = await FindMovieInListAsync(ticket, cancellationToken).ConfigureAwait(false);

        return movie is null ? null : ToTicket(ticket, movie, FindCurrent(unlocked));
    }

    private string? WatchLink(SpookieTicketOptions ticket)
    {
        var link = ticket.Link?.Trim();
        if (string.IsNullOrEmpty(link))
            return null;

        if (Uri.TryCreate(link, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            return link;

        logger.LogWarning(
            "Spookie night ticket {Key} has a watch link that is not an absolute http(s) URL.",
            ticket.Key);
        return null;
    }

    private async Task<Movie?> FindMovieInListAsync(
        SpookieTicketOptions ticket,
        CancellationToken cancellationToken)
    {
        var movie = string.IsNullOrWhiteSpace(_options.WatchlistId) || string.IsNullOrWhiteSpace(ticket.MovieId)
            ? null
            : await movieService
                .GetMovieAsync(_options.WatchlistId, ticket.MovieId, cancellationToken)
                .ConfigureAwait(false);

        if (movie is null)
        {
            logger.LogWarning(
                "Spookie night ticket {Key} references movie {MovieId}, which is not in watchlist {WatchlistId}.",
                ticket.Key,
                ticket.MovieId,
                _options.WatchlistId);
        }

        return movie;
    }

    private List<SpookieTicketOptions> GetUnlockedTicketOptions()
    {
        var today = GetToday();

        return
        [
            .. _options.Tickets.Where(ticket =>
                !string.IsNullOrWhiteSpace(ticket.Key) && ticket.UnlockDate <= today),
        ];
    }

    private DateOnly GetToday()
    {
        var now = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), ResolveTimeZone());
        return DateOnly.FromDateTime(now.DateTime);
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(_options.TimeZone, out var timeZone))
            return timeZone;

        if (_options.TimeZone.Equals("Europe/Kyiv", StringComparison.OrdinalIgnoreCase) &&
            TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Kiev", out timeZone))
            return timeZone;

        throw new InvalidOperationException(
            $"Spookie night time zone '{_options.TimeZone}' was not found.");
    }

    private static SpookieTicketOptions? FindCurrent(IReadOnlyList<SpookieTicketOptions> unlocked) =>
        unlocked.LastOrDefault(ticket =>
            ticket.UnlockDate == unlocked.Max(candidate => candidate.UnlockDate));

    private SpookieTicket ToTicket(
        SpookieTicketOptions ticket,
        Movie movie,
        SpookieTicketOptions? current) =>
        new(ticket.Key, ticket.IsBonus, ReferenceEquals(ticket, current), movie, WatchLink(ticket));
}
