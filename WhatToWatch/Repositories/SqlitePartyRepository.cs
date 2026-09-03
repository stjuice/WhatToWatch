using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatToWatch.Data;
using WhatToWatch.Services;

namespace WhatToWatch.Repositories;

public sealed class SqlitePartyRepository(WhatToWatchDbContext db) : IPartyRepository
{
    public async Task<IReadOnlySet<string>> GetActiveCodesAsync(
        CancellationToken cancellationToken = default) =>
        (await db.Parties.AsNoTracking()
            .Where(party => party.Status == PartyStatus.Playing)
            .Select(party => party.JoinCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
        .ToHashSet(StringComparer.Ordinal);

    public Task<PartyEntity?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(party => party.Id == id, cancellationToken);

    public async Task<PartyEntity?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var parties = await Query()
            .Where(party => party.JoinCode == code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return parties.OrderByDescending(party => party.CreatedAt).FirstOrDefault();
    }

    public async Task AddAsync(
        PartyEntity party,
        CancellationToken cancellationToken = default)
    {
        db.Parties.Add(party);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsConstraintViolation(exception))
        {
            db.Entry(party).State = EntityState.Detached;

            throw PartyException.CodeTaken();
        }
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task CleanupAsync(
        DateTimeOffset now,
        TimeSpan inactivityTimeout,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        var staleBefore = now - inactivityTimeout;
        var active = await Query()
            .Where(party => party.Status == PartyStatus.Playing)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var party in active.Where(party =>
            party.Players.Count == 0 || party.Players.All(player => player.LastSeenAt < staleBefore)))
        {
            party.Status = PartyStatus.Expired;
            party.FinishedAt = now;
        }

        var deleteBefore = now - retention;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var terminal = await db.Parties
            .Where(party => party.Status != PartyStatus.Playing
                && party.FinishedAt != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        db.Parties.RemoveRange(terminal.Where(party => party.FinishedAt < deleteBefore));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PartyEntity> VoteAsync(
        string partyId,
        string playerToken,
        string movieReference,
        bool liked,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        db.ChangeTracker.Clear();

        var connection = (SqliteConnection)db.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        db.Database.UseTransaction(transaction);

        try
        {
            var party = await Query()
            .FirstOrDefaultAsync(item => item.Id == partyId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw PartyException.PartyExpired();

            if (party.Status != PartyStatus.Playing)
                throw party.Status == PartyStatus.Expired
                ? PartyException.PartyExpired()
                : PartyException.AlreadyFinished();

            var player = party.Players.SingleOrDefault(item =>
                CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(item.PlayerToken),
                    System.Text.Encoding.UTF8.GetBytes(playerToken))) 
                ?? throw PartyException.InvalidPlayerToken();

            if (player.CurrentIndex >= player.MovieOrder.Count
                || player.MovieOrder[player.CurrentIndex] != movieReference)
                throw PartyException.NotYourCurrentMovie();

            player.LastSeenAt = now;
            player.CurrentIndex++;

            if (liked)
            {
                var movieId = PartyMovieReference.Decode(movieReference).MovieId;

                if (!party.Likes.Any(item => item.PlayerId == player.Id && item.MovieId == movieId))
                {
                    party.Likes.Add(new PartyLikeEntity
                    {
                        PartyId = party.Id,
                        PlayerId = player.Id,
                        MovieId = movieId,
                        CreatedAt = now,
                    });
                }

                var likedByOther = party.Likes.Any(item =>
                    item.MovieId == movieId && item.PlayerId != player.Id);

                if (likedByOther)
                {
                    var reference = PartyMovieReference.Decode(movieReference);
                    party.Status = PartyStatus.Matched;
                    party.MatchedMovieId = reference.MovieId;
                    party.MatchedWatchlistId = reference.WatchlistId;
                    party.FinishedAt = now;
                }
            }

            if (party.Status == PartyStatus.Playing
                && party.Players.Count == 2
                && party.Players.All(item => item.CurrentIndex >= item.MovieOrder.Count))
            {
                party.Status = PartyStatus.Finished;
                party.FinishedAt = now;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return party;
        }
        finally
        {
            db.Database.UseTransaction(null);
        }
    }

    private IQueryable<PartyEntity> Query() =>
        db.Parties.Include(party => party.Players).Include(party => party.Likes);

    private static bool IsConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: 19 };
}
