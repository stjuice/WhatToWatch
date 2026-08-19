using Microsoft.EntityFrameworkCore;

namespace WhatToWatch.Data;

public static class SqliteSchemaUpgrades
{
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

        if (hasMediaCategory)
            return;

        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Movies"
            ADD COLUMN "MediaCategory" INTEGER NOT NULL DEFAULT 0;
            """,
            cancellationToken).ConfigureAwait(false);
    }
}
