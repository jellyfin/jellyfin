using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to repair date values the database cannot read back, which would otherwise abort every
/// later migration that materialises the row. What can be salvaged is kept to the minute, and the rest
/// is cleared.
/// </summary>
[JellyfinMigration("2025-04-20T20:30:00", nameof(ClearUnreadableDates))]
public class ClearUnreadableDates : IAsyncMigrationRoutine
{
    // Columns that cannot hold NULL take the value a default(DateTime) round trips as.
    private const string UnknownDate = "0001-01-01 00:00:00";

    private readonly ILogger _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClearUnreadableDates"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="startupLogger">The startup logger for Startup UI integration.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    public ClearUnreadableDates(
        ILogger<ClearUnreadableDates> logger,
        IStartupLogger<ClearUnreadableDates> startupLogger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = startupLogger.With(logger);
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var existing = await ReadSchemaAsync(context, cancellationToken).ConfigureAwait(false);
            var repaired = 0;

            foreach (var table in GetDateColumns(context.Model))
            {
                var columns = table.Value.Where(column => existing.Contains((table.Key, column.Name))).ToArray();
                if (columns.Length == 0)
                {
                    continue;
                }

                var sql = BuildRepairStatement(table.Key, columns);

                // Table and column names cannot be parameters. These come from the model rather than from
                // any input, and only after passing the identifier check in GetDateColumns.
#pragma warning disable EF1002
                var affected = await context.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1002

                if (affected > 0)
                {
                    _logger.LogWarning("Cleared unreadable dates in {Count} {Table} rows", affected, table.Key);
                    repaired += affected;
                }
            }

            _logger.LogInformation("Repaired {Count} rows holding a date the database cannot read back", repaired);
        }
    }

    /// <summary>
    /// Builds a single statement per table, because the test is a scan and the largest tables carry
    /// several date columns.
    /// </summary>
    /// <param name="table">The table to repair.</param>
    /// <param name="columns">The date columns of that table.</param>
    /// <returns>The update statement.</returns>
    private static string BuildRepairStatement(string table, IReadOnlyList<(string Name, bool IsNullable)> columns)
    {
        // datetime() is SQLite's own parser: it yields NULL for exactly the values that cannot be read
        // back as a date. Restricting to text values leaves any numeric storage alone.
        static string Unreadable(string column)
            => $"(typeof(\"{column}\") = 'text' AND datetime(\"{column}\") IS NULL)";

        // Corruption usually lands in one field, so the rest of the string is still good. Keeping the
        // part up to the minute costs at most 59 seconds and holds the ordering these columns are read
        // for. SQLite decides whether that part is readable too, and when it is not the value goes.
        static string SalvageToTheMinute(string column)
            => $"datetime(substr(\"{column}\", 1, 16))";

        var assignments = columns.Select(column =>
        {
            var replacement = column.IsNullable ? "NULL" : $"'{UnknownDate}'";
            return $"\"{column.Name}\" = CASE WHEN {Unreadable(column.Name)} THEN coalesce({SalvageToTheMinute(column.Name)}, {replacement}) ELSE \"{column.Name}\" END";
        });

        return $"UPDATE \"{table}\" SET {string.Join(", ", assignments)} WHERE {string.Join(" OR ", columns.Select(column => Unreadable(column.Name)))}";
    }

    private static Dictionary<string, List<(string Name, bool IsNullable)>> GetDateColumns(IModel model)
    {
        var tables = new Dictionary<string, List<(string Name, bool IsNullable)>>(StringComparer.Ordinal);

        foreach (var (table, column, isNullable) in EnumerateDateColumns(model))
        {
            if (!tables.TryGetValue(table, out var columns))
            {
                columns = [];
                tables[table] = columns;
            }

            // Inheritance and owned types can map several entity types onto one table.
            if (!columns.Any(known => string.Equals(known.Name, column, StringComparison.Ordinal)))
            {
                columns.Add((column, isNullable));
            }
        }

        return tables;
    }

    private static IEnumerable<(string Table, string Column, bool IsNullable)> EnumerateDateColumns(IModel model)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (table is null || !IsPlainIdentifier(table))
            {
                continue;
            }

            var storeObject = StoreObjectIdentifier.Table(table, entityType.GetSchema());
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType != typeof(DateTime) && property.ClrType != typeof(DateTime?))
                {
                    continue;
                }

                var column = property.GetColumnName(storeObject);
                if (column is not null && IsPlainIdentifier(column))
                {
                    yield return (table, column, property.IsNullable);
                }
            }
        }
    }

    private static bool IsPlainIdentifier(string value)
        => value.Length > 0 && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    /// <summary>
    /// Reads the columns that exist right now, because the schema this runs against is whatever the
    /// migrations applied so far produced rather than the current model.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Every table and column pair in the database.</returns>
    private static async Task<HashSet<(string Table, string Column)>> ReadSchemaAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        var existing = new HashSet<(string Table, string Column)>();

        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT m.name, p.name FROM sqlite_master AS m JOIN pragma_table_info(m.name) AS p WHERE m.type = 'table'";

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                existing.Add((reader.GetString(0), reader.GetString(1)));
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }

        return existing;
    }
}
