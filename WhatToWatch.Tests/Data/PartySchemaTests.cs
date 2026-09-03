using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatToWatch.Data;

namespace WhatToWatch.Tests.Data;

public class PartySchemaTests
{
    private static readonly string[] PartyTableNames =
        ["Parties", "PartyPlayers", "PartyLikes"];

    [Fact]
    public async Task EnsurePartyTablesAsync_CreatesTablesAndIndexesOnLegacyDatabase()
    {
        await using var connection = await OpenConnectionAsync();
        await CreateLegacyDatabaseAsync(connection);
        await using var db = CreateDbContext(connection);

        await SqliteSchemaUpgrades.EnsurePartyTablesAsync(db);
        await SqliteSchemaUpgrades.EnsurePartyTablesAsync(db);

        Assert.Equal(PartyTableNames.Order(), (await GetPartyTableNamesAsync(connection)).Order());

        var indexes = await GetNamedPartyIndexesAsync(connection);
        Assert.Equal(
            [
                "IX_Parties_JoinCode",
                "IX_PartyLikes_PartyId_MovieId",
                "IX_PartyPlayers_PartyId",
                "IX_PartyPlayers_PlayerToken",
            ],
            indexes);

        var joinCodeIndexSql = await GetSchemaSqlAsync(connection, "IX_Parties_JoinCode");
        Assert.Contains("UNIQUE INDEX", joinCodeIndexSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("""WHERE "Status" = 1""", joinCodeIndexSql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureCreated_FreshDatabaseHasSamePartySchemaAsLegacyUpgrade()
    {
        await using var freshConnection = await OpenConnectionAsync();
        await using var freshDb = CreateDbContext(freshConnection);
        await freshDb.Database.EnsureCreatedAsync();

        await using var legacyConnection = await OpenConnectionAsync();
        await CreateLegacyDatabaseAsync(legacyConnection);
        await using var legacyDb = CreateDbContext(legacyConnection);
        await SqliteSchemaUpgrades.EnsurePartyTablesAsync(legacyDb);

        Assert.Equal(
            await DescribePartySchemaAsync(freshConnection),
            await DescribePartySchemaAsync(legacyConnection));
    }

    [Fact]
    public async Task JoinCode_CanBeReusedAfterHoldingPartyFinishes()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var firstParty = CreateParty("party-1", "1234");
        db.Parties.Add(firstParty);
        await db.SaveChangesAsync();

        firstParty.Status = PartyStatus.Finished;
        firstParty.FinishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        db.Parties.Add(CreateParty("party-2", "1234"));

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Parties.CountAsync(party => party.JoinCode == "1234"));
    }

    [Fact]
    public async Task PartyLikeCompositeKey_RejectsDuplicateLike()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        db.Parties.Add(CreateParty("party-1", "1234"));
        db.PartyLikes.Add(CreateLike());
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        db.PartyLikes.Add(CreateLike());

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static PartyEntity CreateParty(string id, string joinCode) =>
        new()
        {
            Id = id,
            JoinCode = joinCode,
            MovieSet = ["watchlist-1|tt0133093"],
            Status = PartyStatus.Playing,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private static PartyLikeEntity CreateLike() =>
        new()
        {
            PartyId = "party-1",
            PlayerId = "player-1",
            MovieId = "tt0133093",
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static WhatToWatchDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<WhatToWatchDbContext>()
            .UseSqlite(connection)
            .Options;

        return new WhatToWatchDbContext(options);
    }

    private static async Task CreateLegacyDatabaseAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE "Watchlists" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Watchlists" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Url" TEXT NULL,
                "LastRefreshedAt" TEXT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<string>> GetPartyTableNamesAsync(
        SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT "name"
            FROM "sqlite_master"
            WHERE "type" = 'table'
              AND "name" IN ('Parties', 'PartyPlayers', 'PartyLikes')
            ORDER BY "name";
            """;

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        return names;
    }

    private static async Task<IReadOnlyList<string>> GetNamedPartyIndexesAsync(
        SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT "name"
            FROM "sqlite_master"
            WHERE "type" = 'index'
              AND "name" LIKE 'IX_Part%'
            ORDER BY "name";
            """;

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        return names;
    }

    private static async Task<string> GetSchemaSqlAsync(
        SqliteConnection connection,
        string objectName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT "sql"
            FROM "sqlite_master"
            WHERE "name" = $name;
            """;
        command.Parameters.AddWithValue("$name", objectName);

        return (string)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Schema object '{objectName}' was not found."));
    }

    private static async Task<IReadOnlyList<string>> DescribePartySchemaAsync(
        SqliteConnection connection)
    {
        var description = new List<string>();

        foreach (var tableName in PartyTableNames.Order())
        {
            await AppendTableColumnsAsync(connection, tableName, description);
            await AppendForeignKeysAsync(connection, tableName, description);
            await AppendIndexesAsync(connection, tableName, description);
        }

        return description;
    }

    private static async Task AppendTableColumnsAsync(
        SqliteConnection connection,
        string tableName,
        List<string> description)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            description.Add(
                $"column:{tableName}:{reader.GetInt32(0)}:{reader.GetString(1)}:"
                + $"{reader.GetString(2)}:{reader.GetInt32(3)}:"
                + $"{(reader.IsDBNull(4) ? "<null>" : reader.GetString(4))}:"
                + $"{reader.GetInt32(5)}");
        }
    }

    private static async Task AppendForeignKeysAsync(
        SqliteConnection connection,
        string tableName,
        List<string> description)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list(\"{tableName}\");";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            description.Add(
                $"foreign-key:{tableName}:{reader.GetString(2)}:{reader.GetString(3)}:"
                + $"{reader.GetString(4)}:{reader.GetString(6)}");
        }
    }

    private static async Task AppendIndexesAsync(
        SqliteConnection connection,
        string tableName,
        List<string> description)
    {
        var indexes = new List<(string Name, int Unique, string Origin, int Partial)>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA index_list(\"{tableName}\");";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexes.Add((
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetString(3),
                    reader.GetInt32(4)));
            }
        }

        foreach (var index in indexes.OrderBy(item => item.Name))
        {
            var columns = new List<string>();
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA index_info(\"{index.Name}\");";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                columns.Add(reader.GetString(2));

            description.Add(
                $"index:{tableName}:{index.Name}:{index.Unique}:{index.Origin}:"
                + $"{index.Partial}:{string.Join(",", columns)}");
        }
    }
}
