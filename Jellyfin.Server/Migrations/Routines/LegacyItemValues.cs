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

    private const int InsertChunkSize = 200;

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
        return string.Join(',', ids.Select(e => "'" + e.ToString("D", CultureInfo.InvariantCulture).ToUpperInvariant() + "'"));
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
        var typeList = TypeList(types);

        await context.Database
            .ExecuteSqlRawAsync(
                $"DELETE FROM ItemValuesMap WHERE ItemValueId IN (SELECT ItemValueId FROM ItemValues WHERE Type IN ({typeList}))",
                cancellationToken)
            .ConfigureAwait(false);

        return await context.Database
            .ExecuteSqlRawAsync($"DELETE FROM ItemValues WHERE Type IN ({typeList})", cancellationToken)
            .ConfigureAwait(false);
    }

    public static void Write(
        JellyfinDbContext context,
        IReadOnlyList<(Guid ItemValueId, int Type, string Value, string CleanValue, IReadOnlyList<Guid> ItemIds)> values)
    {
        foreach (var chunk in values.Chunk(InsertChunkSize))
        {
            Execute(
                context,
                "INSERT INTO ItemValues (ItemValueId, Type, Value, CleanValue) VALUES ",
                chunk.Length,
                4,
                (parameters, index) =>
                {
                    var value = chunk[index];
                    parameters.Add(value.ItemValueId);
                    parameters.Add(value.Type);
                    parameters.Add(value.Value);
                    parameters.Add(value.CleanValue);
                });
        }

        var maps = values
            .SelectMany(v => v.ItemIds.Select(itemId => (v.ItemValueId, ItemId: itemId)))
            .ToArray();

        foreach (var chunk in maps.Chunk(InsertChunkSize))
        {
            Execute(
                context,
                "INSERT INTO ItemValuesMap (ItemValueId, ItemId) VALUES ",
                chunk.Length,
                2,
                (parameters, index) =>
                {
                    parameters.Add(chunk[index].ItemValueId);
                    parameters.Add(chunk[index].ItemId);
                });
        }
    }

    private static void Execute(
        JellyfinDbContext context,
        string insertInto,
        int rowCount,
        int columnCount,
        Action<List<object>, int> addRow)
    {
        var parameters = new List<object>(rowCount * columnCount);
        var rows = new List<string>(rowCount);

        for (var row = 0; row < rowCount; row++)
        {
            addRow(parameters, row);
            var placeholders = Enumerable
                .Range(row * columnCount, columnCount)
                .Select(i => "{" + i.ToString(CultureInfo.InvariantCulture) + "}");
            rows.Add("(" + string.Join(',', placeholders) + ")");
        }

        context.Database.ExecuteSqlRaw(insertInto + string.Join(',', rows), parameters);
    }
}
