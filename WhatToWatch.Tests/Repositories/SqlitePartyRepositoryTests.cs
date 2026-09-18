using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatToWatch.Data;
using WhatToWatch.Repositories;
using WhatToWatch.Services;

namespace WhatToWatch.Tests.Repositories;

public sealed class SqlitePartyRepositoryTests
{
    [Fact]
    public async Task ConcurrentYesVotes_CreateOneValidMatch()
    {
        var path = Path.Combine(Path.GetTempPath(), $"whattowatch-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<WhatToWatchDbContext>()
                .UseSqlite($"Data Source={path};Default Timeout=10")
                .Options;
            await using (var setup = new WhatToWatchDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.Parties.Add(CreateParty());
                await setup.SaveChangesAsync();
            }

            await using var db1 = new WhatToWatchDbContext(options);
            await using var db2 = new WhatToWatchDbContext(options);
            var repository1 = new SqlitePartyRepository(db1);
            var repository2 = new SqlitePartyRepository(db2);
            var now = DateTimeOffset.UtcNow;

            var votes = await Task.WhenAll(
                repository1.VoteAsync("party", "token-one", "tt1", true, now),
                repository2.VoteAsync("party", "token-two", "tt1", true, now));

            Assert.Contains(votes, party => party.Status == PartyStatus.Matched);
            await using var verify = new WhatToWatchDbContext(options);
            var party = await verify.Parties.Include(item => item.Likes).SingleAsync();
            Assert.Equal(PartyStatus.Matched, party.Status);
            Assert.Equal("tt1", party.MatchedMovieId);
            Assert.Equal("list", party.MatchedWatchlistId);
            Assert.Equal(2, party.Likes.Count);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cleanup_DeletesTerminalPartiesPastRetention()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WhatToWatchDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new WhatToWatchDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var old = DateTimeOffset.UtcNow.AddDays(-2);
        db.Parties.Add(new PartyEntity
        {
            Id = "old",
            JoinCode = "123",
            MovieSet = [],
            Status = PartyStatus.Finished,
            CreatedAt = old,
            FinishedAt = old,
        });
        await db.SaveChangesAsync();

        await new SqlitePartyRepository(db).CleanupAsync(
            DateTimeOffset.UtcNow,
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(24));

        Assert.Empty(await db.Parties.ToListAsync());
    }

    private static PartyEntity CreateParty()
    {
        var reference = PartyMovieReference.Encode("list", "tt1");
        var now = DateTimeOffset.UtcNow;
        return new PartyEntity
        {
            Id = "party",
            JoinCode = "123",
            MovieSet = [reference],
            Status = PartyStatus.Playing,
            CreatedAt = now,
            Players =
            [
                Player("one", "token-one", 1, reference, now),
                Player("two", "token-two", 2, reference, now),
            ],
        };
    }

    private static PartyPlayerEntity Player(
        string id,
        string token,
        int slot,
        string reference,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            PartyId = "party",
            PlayerToken = token,
            Slot = slot,
            MovieOrder = [reference],
            JoinedAt = now,
            LastSeenAt = now,
        };
}
