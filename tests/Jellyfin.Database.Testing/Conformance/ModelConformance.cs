using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: the model of the selected provider has the tables, columns, keys, relationships and indexes of the
/// SQLite model, whatever conventions the provider adds; and every table of the schema the migrations build stores a
/// row with a value in every column and gives it back unchanged.
/// </summary>
/// <remarks>
/// <para>
/// An engine may differ from the SQLite model only where its row of <see cref="EngineTraits"/> says so, and exactly
/// there. An index whose filter the engine cannot express (<see cref="EngineTraits.UnexpressibleIndexFilters"/>) is an
/// index over every row, as long as it is not unique and is the same otherwise: that changes how large the index is and
/// how queries use it, never which rows the database accepts. A relationship that deletes in cascade in the SQLite model
/// and that the engine cannot cascade (<see cref="EngineTraits.NoActionCascades"/>) deletes nothing itself; Jellyfin
/// deletes its rows before the rows they point at, which the folder delete row of the kit holds the engine to. A unique
/// index with a column that takes NULL, which on the other engines takes any number of rows with a NULL in it, may be
/// filtered on an engine whose unique index takes one NULL (<see cref="EngineTraits.NullFilteredUniqueIndexes"/>), to the
/// rows where those columns have a value: it then takes the same rows.
/// </para>
/// <para>
/// An allowance must name what the SQLite model has: a filter of an index that is not unique, a relationship that deletes
/// in cascade, or a unique index without a filter that has a column that takes NULL. It must also be used, so the model of
/// the engine differs from the SQLite model at each place it names. Any other difference fails the row.
/// </para>
/// </remarks>
public abstract class ModelConformance : ConformanceTestBase
{
    [ConformanceFact]
    public void Model_OfTheSelectedProvider_HasTheShapeOfTheSqliteModel()
    {
        using var reference = new JellyfinDbContext(
            SqliteTestBackend.CreateOptions("Data Source=:memory:", []),
            NullLogger<JellyfinDbContext>.Instance,
            SqliteTestBackend.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        using var context = CreateDbContext();

        var traits = EngineTraits.For(DbProvider);
        AssertNone(ShapeDifferences(reference.Model, context.Model, traits.ModelAllowances));
    }

    [ConformanceFact]
    public void EveryTable_RowWithAValueInEveryColumn_IsReadBackUnchanged()
    {
        var wrong = new List<string>();
        foreach (var table in Tables())
        {
            var (written, key) = Rows.Save(table);
            using var context = CreateDbContext();
            var read = ModelRowFactory.Find(context, table, key);
            if (read is null)
            {
                wrong.Add($"{table.GetTableName()}: the row is not found by its key");
            }
            else if (!string.Equals(written, ModelRowFactory.Describe(read), StringComparison.Ordinal))
            {
                wrong.Add($"{table.GetTableName()}:{Environment.NewLine}  written {written}{Environment.NewLine}  read    {ModelRowFactory.Describe(read)}");
            }
        }

        AssertNone(wrong);
    }

    /// <summary>
    /// Compares the shape of a model with that of the reference model: its tables, columns, keys, relationships and
    /// indexes.
    /// </summary>
    /// <param name="reference">The model whose shape is expected.</param>
    /// <param name="actual">The model to compare.</param>
    /// <param name="allowances">
    /// Where <paramref name="actual"/> may differ from the reference, and must: each allowance must name what the reference
    /// has, and <paramref name="actual"/> must differ there.
    /// </param>
    /// <returns>The differences, one line each; none when the shapes match.</returns>
    public static IReadOnlyList<string> ShapeDifferences(
        IModel reference,
        IModel actual,
        ModelAllowances allowances)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(allowances);

        var expected = Shape(reference);
        var found = Shape(actual);
        var problems = new List<string>();

        // Each allowance swaps a line of the reference for the line the engine has instead.
        var swaps = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var filter in allowances.UnexpressibleIndexFilters)
        {
            var indexes = ModelRowFactory.Tables(reference)
                .SelectMany(table => table.GetIndexes()
                    .Where(index => string.Equals(index.GetFilter(), filter, StringComparison.Ordinal))
                    .Select(index => (Table: table, Index: index)))
                .ToList();
            if (indexes.Count == 0)
            {
                problems.Add($"allowance for nothing: no index of the reference has the filter {filter}");
            }

            foreach (var (table, index) in indexes)
            {
                if (index.IsUnique)
                {
                    problems.Add($"allowance for a unique index: {IndexLine(table, index, filtered: true)} has the filter {filter}, and without it the index refuses other rows");
                    continue;
                }

                swaps[IndexLine(table, index, filtered: true)] = IndexLine(table, index, filtered: false);
            }
        }

        foreach (var (tableName, columns) in allowances.NoActionCascades)
        {
            var keys = ModelRowFactory.Tables(reference)
                .Where(table => string.Equals(table.GetTableName(), tableName, StringComparison.Ordinal))
                .SelectMany(table => table.GetForeignKeys()
                    .Where(foreignKey => string.Equals(Columns(foreignKey.Properties), columns, StringComparison.Ordinal))
                    .Select(foreignKey => (Table: table, ForeignKey: foreignKey)))
                .ToList();
            if (keys.Count == 0)
            {
                problems.Add($"allowance for nothing: the reference has no foreign key {tableName} ({columns})");
            }

            foreach (var (table, foreignKey) in keys)
            {
                if (foreignKey.DeleteBehavior != DeleteBehavior.Cascade)
                {
                    problems.Add($"allowance for a key that does not cascade: {ForeignKeyLine(table, foreignKey, foreignKey.DeleteBehavior)}");
                    continue;
                }

                swaps[ForeignKeyLine(table, foreignKey, DeleteBehavior.Cascade)] = ForeignKeyLine(table, foreignKey, DeleteBehavior.NoAction);
            }
        }

        foreach (var (tableName, columns, filter) in allowances.NullFilteredUniqueIndexes)
        {
            var indexes = FindIndexes(reference, tableName, columns);
            if (indexes.Count == 0)
            {
                problems.Add($"allowance for nothing: the reference has no index {tableName} ({columns})");
            }

            foreach (var (table, index) in indexes)
            {
                // As EF Core does for SQL Server: a unique index without a filter that has a column that takes NULL.
                if (!index.IsUnique || index.GetFilter() is not null || !index.Properties.Any(property => property.IsNullable))
                {
                    problems.Add($"allowance for an index that is not unique, is filtered, or has no column that takes NULL: {IndexLine(table, index, filtered: index.GetFilter() is not null)}");
                    continue;
                }

                swaps[IndexLine(table, index, filtered: false)] = IndexLine(table, index, filtered: true);
                var actualFilters = FindIndexes(actual, tableName, columns).Select(x => x.Index.GetFilter()).ToList();
                if (actualFilters.Any(actualFilter => actualFilter is not null && !string.Equals(actualFilter, filter, StringComparison.Ordinal)))
                {
                    problems.Add($"allowance for another filter: {IndexLine(table, index, filtered: true)} has the filter {string.Join(", ", actualFilters)}, not {filter}");
                }
            }
        }

        // An allowance the model does not use is reported once, and its line is compared as the reference has it.
        foreach (var (line, allowed) in swaps.ToList())
        {
            if (found.Contains(line, StringComparer.Ordinal) || !found.Contains(allowed, StringComparer.Ordinal))
            {
                problems.Add($"allowance not used: expected {allowed} instead of {line}");
                swaps.Remove(line);
            }
        }

        expected = expected
            .Select(line => swaps.TryGetValue(line, out var allowed) ? allowed : line)
            .Order(StringComparer.Ordinal)
            .ToList();

        problems.AddRange(Minus(expected, found).Select(line => $"missing: {line}"));
        problems.AddRange(Minus(found, expected).Select(line => $"not in the reference: {line}"));
        return problems;
    }

    private static List<(IEntityType Table, IIndex Index)> FindIndexes(IModel model, string tableName, string columns)
        => ModelRowFactory.Tables(model)
            .Where(table => string.Equals(table.GetTableName(), tableName, StringComparison.Ordinal))
            .SelectMany(table => table.GetIndexes()
                .Where(index => string.Equals(Columns(index.Properties), columns, StringComparison.Ordinal))
                .Select(index => (table, index)))
            .ToList();

    // The lines of the first list that the second does not have, as often as the first has them more.
    private static List<string> Minus(IReadOnlyList<string> lines, IReadOnlyList<string> others)
    {
        var remaining = others.GroupBy(line => line, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var line in lines)
        {
            if (remaining.TryGetValue(line, out var count) && count > 0)
            {
                remaining[line] = count - 1;
            }
            else
            {
                result.Add(line);
            }
        }

        return result;
    }

    private static string Columns(IEnumerable<IProperty> properties) => string.Join(", ", properties.Select(property => property.GetColumnName()));

    private static string IndexLine(IEntityType table, IIndex index, bool filtered)
        => $"{table.GetTableName()} {(index.IsUnique ? "unique index" : "index")} ({Columns(index.Properties)}){(filtered ? ", filtered" : string.Empty)}";

    private static string ForeignKeyLine(IEntityType table, IForeignKey foreignKey, DeleteBehavior deleteBehavior)
        => $"{table.GetTableName()} foreign key ({Columns(foreignKey.Properties)}) -> {foreignKey.PrincipalEntityType.GetTableName()} ({Columns(foreignKey.PrincipalKey.Properties)}), {(foreignKey.IsRequired ? "required" : "optional")}, on delete {deleteBehavior}";

    private static List<string> Shape(IModel model)
    {
        var lines = new List<string>();
        foreach (var table in ModelRowFactory.Tables(model))
        {
            var name = table.GetTableName();

            foreach (var property in table.GetProperties())
            {
                lines.Add($"{name} column {property.GetColumnName()}: {property.ClrType.Name}, {(property.IsNullable ? "null" : "not null")}, max {property.GetMaxLength()?.ToString(CultureInfo.InvariantCulture) ?? "none"}");
            }

            lines.Add($"{name} primary key ({Columns(table.FindPrimaryKey()!.Properties)})");
            foreach (var foreignKey in table.GetForeignKeys())
            {
                lines.Add(ForeignKeyLine(table, foreignKey, foreignKey.DeleteBehavior));
            }

            foreach (var index in table.GetIndexes())
            {
                lines.Add(IndexLine(table, index, filtered: index.GetFilter() is not null));
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }
}
