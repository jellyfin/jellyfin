using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

/// <summary>
/// The operations of the migrations of the providers, reduced to what is compared across them:
/// the kind of each operation, its table and its columns, whether a column is nullable, whether an index is unique,
/// what a foreign key does on delete, and the text of SQL.
/// </summary>
/// <remarks>
/// Not compared, because they are engine details: annotations, column types and maximum lengths, defaults and computed column SQL,
/// collations, index filters, the SQL of check constraints, the names of new indexes and constraints, the values of data operations,
/// and the order of the operations. A hand edit to one of them in one provider's migration, for example to a column type, is caught
/// by neither this check nor the pending-changes check, which compares the model snapshot with the model and not the body of the migration.
/// </remarks>
internal static partial class MigrationContents
{
    /// <summary>
    /// The direction of a migration that runs when it is applied.
    /// </summary>
    public const string Up = "Up";

    /// <summary>
    /// The direction of a migration that runs when it is reverted.
    /// </summary>
    public const string Down = "Down";

    private const int SqlExcerptLength = 60;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    /// <summary>
    /// Gets the rows of the provider table whose migrations are compared: the first row of each migrations assembly.
    /// MySQL and MariaDB share one set of migrations, which the table lists at several server versions because their models differ.
    /// The operations of a migration do not depend on the version, which <c>RowsThatShareMigrations_HaveTheSameOperations</c> checks,
    /// so the set is compared once, under the name of its first row.
    /// </summary>
    /// <returns>The rows.</returns>
    public static IReadOnlyList<MigrationProvider> MigrationSets()
    {
        return MigrationProvider.All.DistinctBy(MigrationsAssembly).ToList();
    }

    /// <summary>
    /// Gets the assembly that holds the migrations of a provider.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The assembly.</returns>
    public static Assembly MigrationsAssembly(MigrationProvider provider)
    {
        using var context = provider.CreateContext();
        return context.GetService<IMigrationsAssembly>().Assembly;
    }

    /// <summary>
    /// Builds the operations of every migration of a provider. Nothing connects to a database.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The operations of each migration, in the order of the ids.</returns>
    public static IEnumerable<MigrationOperations> Load(MigrationProvider provider)
    {
        using var context = provider.CreateContext();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var activeProvider = context.Database.ProviderName
            ?? throw new InvalidOperationException($"The context of {provider.Name} has no database provider.");

        var result = new List<MigrationOperations>();
        foreach (var (id, type) in migrationsAssembly.Migrations.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            var migration = migrationsAssembly.CreateMigration(type, activeProvider);
            result.Add(new MigrationOperations(provider.Name, id, migration.UpOperations, migration.DownOperations));
        }

        return result;
    }

    /// <summary>
    /// Finds the differences between the migrations that share an id. The order of the operations is not compared.
    /// </summary>
    /// <param name="migrations">The migrations of every provider, with the providers in the order of their table.</param>
    /// <returns>The differences, one for each operation and group of providers that have it more often than the others.</returns>
    public static IReadOnlyList<MigrationDifference> Compare(IEnumerable<MigrationOperations> migrations)
    {
        var differences = new List<MigrationDifference>();
        foreach (var paired in migrations.GroupBy(e => e.MigrationId).Where(e => e.Count() > 1).OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            var providers = paired.ToList();
            if (providers.Select(e => e.Provider).Distinct(StringComparer.Ordinal).Count() != providers.Count)
            {
                throw new InvalidOperationException($"A provider has the migration {paired.Key} more than once.");
            }

            foreach (var direction in new[] { Up, Down })
            {
                var counts = providers
                    .Select(e => (direction == Up ? e.Up : e.Down)
                        .SelectMany(Describe)
                        .GroupBy(d => d, StringComparer.Ordinal)
                        .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal))
                    .ToList();

                foreach (var operation in counts.SelectMany(e => e.Keys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                {
                    var occurrences = counts.Select(e => e.GetValueOrDefault(operation)).ToList();
                    var least = occurrences.Min();
                    foreach (var extra in occurrences.Where(e => e > least).Distinct().OrderDescending())
                    {
                        var owners = providers.Where((_, i) => occurrences[i] == extra).Select(e => e.Provider);
                        var times = extra - least;
                        differences.Add(new MigrationDifference(
                            paired.Key,
                            direction,
                            string.Join(", ", owners),
                            times == 1 ? operation : operation + string.Create(CultureInfo.InvariantCulture, $" (x{times})")));
                    }
                }
            }
        }

        return differences;
    }

    /// <summary>
    /// Describes an operation as what is compared across providers.
    /// A table is described by one entry for the table and one for each of its columns and keys, so that a difference names the column.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <returns>The descriptions.</returns>
    public static IEnumerable<string> Describe(MigrationOperation operation)
    {
        return operation switch
        {
            CreateTableOperation table => DescribeTable(table),
            DropTableOperation table => [$"DropTable {table.Name}"],
            RenameTableOperation table => [$"RenameTable {table.Name} -> {table.NewName}"],
            AddColumnOperation column => [$"AddColumn {column.Table}.{column.Name} {Nullability(column.IsNullable)}"],
            DropColumnOperation column => [$"DropColumn {column.Table}.{column.Name}"],
            AlterColumnOperation column => [$"AlterColumn {column.Table}.{column.Name} {Nullability(column.IsNullable)}"],
            RenameColumnOperation column => [$"RenameColumn {column.Table}.{column.Name} -> {column.NewName}"],
            CreateIndexOperation index => [$"CreateIndex {index.Table} ({Columns(index.Columns)}){(index.IsUnique ? " unique" : string.Empty)}"],
            DropIndexOperation index => [$"DropIndex {index.Table} {index.Name}"],
            RenameIndexOperation index => [$"RenameIndex {index.Table} {index.Name} -> {index.NewName}"],
            AddPrimaryKeyOperation key => [$"AddPrimaryKey {key.Table} ({Columns(key.Columns)})"],
            DropPrimaryKeyOperation key => [$"DropPrimaryKey {key.Table} {key.Name}"],
            AddForeignKeyOperation key => [$"AddForeignKey {key.Table} ({Columns(key.Columns)}) -> {key.PrincipalTable} ({Columns(key.PrincipalColumns)}) on delete {key.OnDelete}"],
            DropForeignKeyOperation key => [$"DropForeignKey {key.Table} {key.Name}"],
            AddUniqueConstraintOperation constraint => [$"AddUniqueConstraint {constraint.Table} ({Columns(constraint.Columns)})"],
            DropUniqueConstraintOperation constraint => [$"DropUniqueConstraint {constraint.Table} {constraint.Name}"],
            AddCheckConstraintOperation constraint => [$"AddCheckConstraint {constraint.Table} {constraint.Name}"],
            DropCheckConstraintOperation constraint => [$"DropCheckConstraint {constraint.Table} {constraint.Name}"],
            InsertDataOperation data => [$"InsertData {data.Table} ({Columns(data.Columns)})"],
            UpdateDataOperation data => [$"UpdateData {data.Table} ({Columns(data.KeyColumns)}) set ({Columns(data.Columns)})"],
            DeleteDataOperation data => [$"DeleteData {data.Table} ({Columns(data.KeyColumns)})"],
            SqlOperation sql => [DescribeSql(sql.Sql)],
            _ => [DescribeOther(operation)]
        };
    }

    /// <summary>
    /// Describes SQL by a hash of its text, with runs of white space made one space, and the start of that text to read.
    /// </summary>
    /// <param name="sql">The SQL.</param>
    /// <returns>The description.</returns>
    public static string DescribeSql(string sql)
    {
        var normalised = Whitespace().Replace(sql, " ").Trim();
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalised)))[..16];
        var excerpt = normalised.Length <= SqlExcerptLength ? normalised : normalised[..SqlExcerptLength] + "...";
        return $"Sql {hash} {excerpt}";
    }

    private static IEnumerable<string> DescribeTable(CreateTableOperation table)
    {
        yield return $"CreateTable {table.Name}";
        foreach (var column in table.Columns)
        {
            yield return $"CreateTable {table.Name} column {column.Name} {Nullability(column.IsNullable)}";
        }

        if (table.PrimaryKey is not null)
        {
            yield return $"CreateTable {table.Name} primary key ({Columns(table.PrimaryKey.Columns)})";
        }

        foreach (var key in table.ForeignKeys)
        {
            yield return $"CreateTable {table.Name} foreign key ({Columns(key.Columns)}) -> {key.PrincipalTable} ({Columns(key.PrincipalColumns)}) on delete {key.OnDelete}";
        }

        foreach (var constraint in table.UniqueConstraints)
        {
            yield return $"CreateTable {table.Name} unique ({Columns(constraint.Columns)})";
        }

        foreach (var constraint in table.CheckConstraints)
        {
            yield return $"CreateTable {table.Name} check {constraint.Name}";
        }
    }

    private static string DescribeOther(MigrationOperation operation)
    {
        var kind = operation.GetType().Name;
        kind = kind.EndsWith("Operation", StringComparison.Ordinal) ? kind[..^"Operation".Length] : kind;
        var table = (operation as ITableMigrationOperation)?.Table;
        var name = operation.GetType().GetProperty("Name")?.GetValue(operation) as string;
        return string.Join(' ', new[] { kind, table, name }.Where(e => !string.IsNullOrEmpty(e)));
    }

    private static string Columns(string[]? columns) => columns is null ? string.Empty : string.Join(", ", columns);

    private static string Nullability(bool isNullable) => isNullable ? "null" : "not null";
}
