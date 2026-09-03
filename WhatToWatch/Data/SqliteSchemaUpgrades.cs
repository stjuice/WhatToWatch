using Microsoft.EntityFrameworkCore;

namespace WhatToWatch.Data;

public static class SqliteSchemaUpgrades
{
    public static Task EnsurePartyTablesAsync(
        WhatToWatchDbContext db,
        CancellationToken cancellationToken = default) =>
        db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "Parties" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Parties" PRIMARY KEY,
                "JoinCode" TEXT NOT NULL,
                "WatchlistId" TEXT NULL,
                "MovieSetJson" TEXT NOT NULL,
                "Status" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "FinishedAt" TEXT NULL,
                "MatchedMovieId" TEXT NULL,
                "MatchedWatchlistId" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "PartyPlayers" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_PartyPlayers" PRIMARY KEY,
                "PartyId" TEXT NOT NULL,
                "PlayerToken" TEXT NOT NULL,
                "Slot" INTEGER NOT NULL,
                "MovieOrderJson" TEXT NOT NULL,
                "CurrentIndex" INTEGER NOT NULL,
                "JoinedAt" TEXT NOT NULL,
                "LastSeenAt" TEXT NOT NULL,
                CONSTRAINT "FK_PartyPlayers_Parties_PartyId"
                    FOREIGN KEY ("PartyId") REFERENCES "Parties" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "PartyLikes" (
                "PartyId" TEXT NOT NULL,
                "PlayerId" TEXT NOT NULL,
                "MovieId" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "PK_PartyLikes" PRIMARY KEY ("PartyId", "PlayerId", "MovieId"),
                CONSTRAINT "FK_PartyLikes_Parties_PartyId"
                    FOREIGN KEY ("PartyId") REFERENCES "Parties" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Parties_JoinCode"
                ON "Parties" ("JoinCode")
                WHERE "Status" = 1;

            CREATE INDEX IF NOT EXISTS "IX_PartyLikes_PartyId_MovieId"
                ON "PartyLikes" ("PartyId", "MovieId");

            CREATE INDEX IF NOT EXISTS "IX_PartyPlayers_PartyId"
                ON "PartyPlayers" ("PartyId");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PartyPlayers_PlayerToken"
                ON "PartyPlayers" ("PlayerToken");
            """,
            cancellationToken);

    public static async Task EnsureMediaCategoryColumnAsync(
        WhatToWatchDbContext db,
        CancellationToken cancellationToken = default)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        command.CommandText = "PRAGMA table_info('Movies');";
        var hasMediaCategory = false;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var columnName = reader.GetString(1);
                if (string.Equals(columnName, "MediaCategory", StringComparison.OrdinalIgnoreCase))
                {
                    hasMediaCategory = true;
                    break;
                }
            }
        }

        if (!hasMediaCategory)
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Movies"
                ADD COLUMN "MediaCategory" INTEGER NOT NULL DEFAULT 1;
                """,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE "Movies"
            SET "MediaCategory" = 1
            WHERE "MediaCategory" = 0;
            """,
            cancellationToken).ConfigureAwait(false);
    }
}
