// The type numbers below are constants of this file, not caller input.
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection
#pragma warning disable EF1003 // Risk of vulnerability to SQL injection
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Jellyfin.Server.Migrations.Routines;

// The value rows an older server wrote, reached by hand because nothing maps them any more.
internal static class LegacyItemValues
{
    public const int Artist = 0;

    public const int AlbumArtist = 1;

    public const int Genre = 2;

    public const int Studio = 3;

    public const int Tag = 4;

    public const int InheritedTag = 6;

    // How many distinct values one pass converts, bounding what a routine holds at once.
    public const int ValueChunkSize = 500;

    public static async Task<List<LegacyItemValue>> ReadValuesAsync(
        JellyfinDbContext context,
        IReadOnlyList<int> types,
        CancellationToken cancellationToken)
    {
        var values = new List<LegacyItemValue>();

        await ReadAsync(
            context,
            $"""
             SELECT ItemValueId, Type, Value
             FROM ItemValues
             WHERE Type IN ({TypeList(types)})
             """,
            reader => values.Add(new LegacyItemValue(reader.GetGuid(0), reader.GetInt32(1), reader.GetString(2))),
            cancellationToken).ConfigureAwait(false);

        return values;
    }

    public static async Task<List<LegacyItemValueLink>> ReadLinksAsync(
        JellyfinDbContext context,
        IReadOnlyList<LegacyItemValue> values,
        CancellationToken cancellationToken)
    {
        var links = new List<LegacyItemValueLink>();
        if (values.Count == 0)
        {
            return links;
        }

        var byId = values.ToDictionary(e => e.ItemValueId);

        await ReadAsync(
            context,
            $"""
             SELECT m.ItemValueId, m.ItemId, b.Type
             FROM ItemValuesMap m
             JOIN BaseItems b ON b.Id = m.ItemId
             WHERE m.ItemValueId IN ({GuidList(values.Select(e => e.ItemValueId))})
             """,
            reader =>
            {
                if (byId.TryGetValue(reader.GetGuid(0), out var value))
                {
                    links.Add(new LegacyItemValueLink(value, reader.GetGuid(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
                }
            },
            cancellationToken).ConfigureAwait(false);

        return links;
    }

    private static async Task ReadAsync(
        JellyfinDbContext context,
        string sql,
        Action<DbDataReader> read,
        CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var command = context.Database.GetDbConnection().CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = sql;

                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        read(reader);
                    }
                }
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private static string TypeList(IEnumerable<int> types)
    {
        return string.Join(',', types.Select(e => e.ToString(CultureInfo.InvariantCulture)));
    }

    // Literals rather than parameters, to stay within SQLite's parameter limit. Uppercase because that
    // is how the provider writes a Guid to TEXT and SQLite compares text case-sensitively.
    private static string GuidList(IEnumerable<Guid> ids)
    {
        return string.Join(',', ids.Select(e => "'" + GuidText(e) + "'"));
    }

    // The tables outlive the migration that took them out of the model, which runs a stage earlier
    // than everything reading them.
    public static async Task DropTablesAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        await context.Database
            .ExecuteSqlRawAsync("DROP TABLE IF EXISTS ItemValuesMap", cancellationToken)
            .ConfigureAwait(false);

        await context.Database
            .ExecuteSqlRawAsync("DROP TABLE IF EXISTS ItemValues", cancellationToken)
            .ConfigureAwait(false);
    }

    public static void DeleteAll(JellyfinDbContext context)
    {
        context.Database.ExecuteSqlRaw("DELETE FROM ItemValuesMap");
        context.Database.ExecuteSqlRaw("DELETE FROM ItemValues");
    }

    public static async Task<int> DeleteAsync(
        JellyfinDbContext context,
        IReadOnlyList<int> types,
        CancellationToken cancellationToken)
    {
        var removed = 0;
        foreach (var type in types)
        {
            await context.Database
                .ExecuteSqlAsync(
                    $"DELETE FROM ItemValuesMap WHERE ItemValueId IN (SELECT ItemValueId FROM ItemValues WHERE Type = {type})",
                    cancellationToken)
                .ConfigureAwait(false);

            removed += await context.Database
                .ExecuteSqlAsync($"DELETE FROM ItemValues WHERE Type = {type}", cancellationToken)
                .ConfigureAwait(false);
        }

        return removed;
    }

    public static void Write(
        JellyfinDbContext context,
        IReadOnlyList<(Guid ItemValueId, int Type, string Value, string CleanValue, IReadOnlyList<Guid> ItemIds)> values)
    {
        context.Database.OpenConnection();
        try
        {
            var transaction = context.Database.CurrentTransaction is null ? context.Database.BeginTransaction() : null;
            using (transaction)
            {
                Insert(
                    context,
                    "INSERT INTO ItemValues (ItemValueId, Type, Value, CleanValue) VALUES ($p0, $p1, $p2, $p3)",
                    values.Select(v => new object[] { GuidText(v.ItemValueId), v.Type, v.Value, v.CleanValue }));

                Insert(
                    context,
                    "INSERT INTO ItemValuesMap (ItemValueId, ItemId) VALUES ($p0, $p1)",
                    values.SelectMany(v => v.ItemIds.Select(itemId => new object[] { GuidText(v.ItemValueId), GuidText(itemId) })));

                transaction?.Commit();
            }
        }
        finally
        {
            context.Database.CloseConnection();
        }
    }

    // One prepared statement for every row, inside the caller's transaction or the one Write opened.
    private static void Insert(JellyfinDbContext context, string sql, IEnumerable<object[]> rows)
    {
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        foreach (var row in rows)
        {
            if (command.Parameters.Count == 0)
            {
                for (var i = 0; i < row.Length; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "$p" + i.ToString(CultureInfo.InvariantCulture);
                    command.Parameters.Add(parameter);
                }

                command.Prepare();
            }

            for (var i = 0; i < row.Length; i++)
            {
                command.Parameters[i].Value = row[i];
            }

            command.ExecuteNonQuery();
        }
    }

    // How the provider writes a Guid to TEXT, which is what every other table compares against.
    private static string GuidText(Guid id)
    {
        return id.ToString("D", CultureInfo.InvariantCulture).ToUpperInvariant();
    }
}
