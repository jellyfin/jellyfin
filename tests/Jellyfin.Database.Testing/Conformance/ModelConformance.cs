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
/// An engine that cannot create a partial index (<see cref="EngineTraits.SupportsPartialIndexes"/>) may have a
/// filtered index of the SQLite model as an index over every row, as long as the index is not unique and is the same
/// otherwise: that changes how large the index is and how queries use it, never which rows the database accepts. A
/// unique index without its filter, or any other difference, still fails the row.
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

        AssertNone(ShapeDifferences(reference.Model, context.Model, EngineTraits.For(DbProvider).SupportsPartialIndexes));
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
    /// <param name="supportsPartialIndexes">
    /// Whether the engine of <paramref name="actual"/> can create a partial index. When it cannot, a filtered index of
    /// the reference that is not unique may be without its filter, and nothing else may differ; the reference must
    /// then have such an index, so that the allowance is not one for nothing.
    /// </param>
    /// <returns>The differences, one line each; none when the shapes match.</returns>
    public static IReadOnlyList<string> ShapeDifferences(IModel reference, IModel actual, bool supportsPartialIndexes)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(actual);

        var expected = Shape(reference);
        var found = Shape(actual);
        var problems = new List<string>();
        if (!supportsPartialIndexes)
        {
            var mayLoseTheirFilter = NonUniqueFilteredIndexes(reference);
            if (mayLoseTheirFilter.Count == 0)
            {
                problems.Add("The engine cannot create a partial index, but the reference model has no filtered index that is not unique, so there is nothing for the allowance to allow.");
            }

            // The line of such an index is expected without its filter instead, where the actual model has it so.
            expected = expected
                .Select(line => mayLoseTheirFilter.TryGetValue(line, out var unfiltered) && !found.Contains(line) && found.Contains(unfiltered) ? unfiltered : line)
                .Order(StringComparer.Ordinal)
                .ToList();
        }

        problems.AddRange(Minus(expected, found).Select(line => $"missing: {line}"));
        problems.AddRange(Minus(found, expected).Select(line => $"not in the reference: {line}"));
        return problems;
    }

    private static Dictionary<string, string> NonUniqueFilteredIndexes(IModel model)
    {
        var lines = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var table in ModelRowFactory.Tables(model))
        {
            foreach (var index in table.GetIndexes().Where(index => !index.IsUnique && index.GetFilter() is not null))
            {
                lines[IndexLine(table, index, filtered: true)] = IndexLine(table, index, filtered: false);
            }
        }

        return lines;
    }

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
                lines.Add($"{name} foreign key ({Columns(foreignKey.Properties)}) -> {foreignKey.PrincipalEntityType.GetTableName()} ({Columns(foreignKey.PrincipalKey.Properties)}), {(foreignKey.IsRequired ? "required" : "optional")}, on delete {foreignKey.DeleteBehavior}");
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
