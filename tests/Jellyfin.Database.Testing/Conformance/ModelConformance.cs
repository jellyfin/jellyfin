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

        var expected = Shape(reference.Model);
        var actual = Shape(context.Model);

        Assert.Equal(expected.Except(actual), actual.Except(expected));
        Assert.Equal(expected, actual);
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

    private static List<string> Shape(IModel model)
    {
        var lines = new List<string>();
        foreach (var table in ModelRowFactory.Tables(model))
        {
            var name = table.GetTableName();
            string Columns(IEnumerable<IProperty> properties) => string.Join(", ", properties.Select(property => property.GetColumnName()));

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
                lines.Add($"{name} {(index.IsUnique ? "unique index" : "index")} ({Columns(index.Properties)}){(index.GetFilter() is null ? string.Empty : ", filtered")}");
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }
}
