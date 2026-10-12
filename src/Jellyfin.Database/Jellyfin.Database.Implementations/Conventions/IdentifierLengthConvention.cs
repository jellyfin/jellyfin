using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Jellyfin.Database.Implementations.Conventions;

/// <summary>
/// Shortens every table, column, key, foreign key and index name that is longer than a maximum length.
/// </summary>
/// <remarks>
/// <para>
/// The length is measured in UTF-8 bytes, which is how PostgreSQL counts it. A name over the limit becomes its leading
/// characters, cut on a character boundary so that the result fits, an underscore and the first eight hexadecimal
/// characters of the SHA-256 hash of the full name. Unlike the truncation of the providers, which numbers colliding
/// names in the order the model was built, the result depends on the name alone.
/// </para>
/// <para>
/// A name that was chosen explicitly or by a data annotation is shortened from that name. For any other name the
/// default name is rebuilt from the full table and column names, which are the names of the entity types and
/// properties before the provider cut them. A truncated name of the provider and a suffix it added to resolve a
/// collision of truncated names are therefore never kept. If two objects of the same kind end up with the same name
/// in the same scope, the model is rejected instead of being given a numbered name.
/// </para>
/// </remarks>
internal sealed class IdentifierLengthConvention : IModelFinalizingConvention
{
    private const int HashLength = 8;

    private readonly int _maxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentifierLengthConvention"/> class.
    /// </summary>
    /// <param name="maxLength">The longest name the database accepts, in UTF-8 bytes.</param>
    public IdentifierLengthConvention(int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, HashLength + 2);
        _maxLength = maxLength;
    }

    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var entityTypes = modelBuilder.Metadata.GetEntityTypes()
            .Where(e => e.GetTableName() is not null)
            .ToList();

        // Every name is worked out before one is changed, because the default names are built from the other names.
        var tables = FullTableNames(entityTypes);
        var names = new List<Identifier>();
        foreach (var entityType in entityTypes)
        {
            var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
            var tableScope = $"table '{Qualified(table)}'";
            var schemaScope = SchemaScope(table.Schema);

            foreach (var key in entityType.GetDeclaredKeys())
            {
                var configured = IsChosen(key.GetNameConfigurationSource());
                names.Add(Plan("key", "index", schemaScope, tableScope, entityType, key.GetName()!, DefaultKeyName(key, table, tables), configured, name => key.SetName(name, false)));
            }

            foreach (var foreignKey in entityType.GetDeclaredForeignKeys())
            {
                var configured = IsChosen(foreignKey.GetConstraintNameConfigurationSource());
                names.Add(Plan("foreign key", "foreign key", tableScope, tableScope, entityType, foreignKey.GetConstraintName()!, DefaultForeignKeyName(foreignKey, table, tables), configured, name => foreignKey.SetConstraintName(name, false)));
            }

            foreach (var index in entityType.GetDeclaredIndexes())
            {
                // The name of the index in the model, for example from the Index attribute, is its database name as well.
                var configured = index.Name is not null || IsChosen(index.GetDatabaseNameConfigurationSource());
                names.Add(Plan("index", "index", schemaScope, tableScope, entityType, index.GetDatabaseName()!, DefaultIndexName(index, table, tables), configured, name => index.SetDatabaseName(name, false)));
            }

            foreach (var property in entityType.GetDeclaredProperties())
            {
                // A property that shares its column with the property of another entity type is named by that one.
                if (property.FindSharedStoreObjectRootProperty(table) is not null)
                {
                    continue;
                }

                var hasOverride = property.GetColumnNameConfigurationSource(table) is not null;
                var configured = IsChosen(property.GetColumnNameConfigurationSource()) || IsChosen(property.GetColumnNameConfigurationSource(table));
                names.Add(Plan(
                    "column",
                    "column",
                    tableScope,
                    tableScope,
                    entityType,
                    property.GetColumnName(table)!,
                    FullColumnName(property, table),
                    configured,
                    name => _ = hasOverride ? property.SetColumnName(name, table, false) : property.SetColumnName(name, false)));
            }
        }

        // Entity types that share a table are renamed together, and the table is checked once.
        foreach (var group in entityTypes.Where(OwnsItsTable).GroupBy(e => (e.GetSchema(), e.GetTableName()!)))
        {
            var current = group.Key.Item2;
            var configured = group.Any(e => IsChosen(e.GetTableNameConfigurationSource()));
            names.Add(Plan("table", "table", SchemaScope(group.Key.Item1), null, null, current, tables[group.Key], configured, name =>
            {
                foreach (var entityType in group)
                {
                    entityType.SetTableName(name, false);
                }
            }));
        }

        ThrowOnCollision(names);

        foreach (var name in names.Where(n => n.Rename is not null))
        {
            name.Rename!(name.Final);
        }
    }

    private static bool IsChosen(ConfigurationSource? source)
    {
        return source is ConfigurationSource.Explicit or ConfigurationSource.DataAnnotation;
    }

    private static bool OwnsItsTable(IConventionEntityType entityType)
    {
        // A derived type that is mapped to the table of its base type has no table of its own to name.
        return entityType.BaseType is null || entityType.GetTableName() != entityType.BaseType.GetTableName();
    }

    private static string Qualified(StoreObjectIdentifier table)
    {
        return table.Schema is null ? table.Name : $"{table.Schema}.{table.Name}";
    }

    private static string SchemaScope(string? schema)
    {
        return schema is null ? "the default schema" : $"schema '{schema}'";
    }

    // The default names of the provider are built from names that it has already cut to its own limit, so the full
    // names are worked out here instead.
    private static Dictionary<(string? Schema, string Name), string> FullTableNames(IEnumerable<IConventionEntityType> entityTypes)
    {
        var tables = new Dictionary<(string?, string), string>();
        foreach (var group in entityTypes.Where(OwnsItsTable).GroupBy(e => (e.GetSchema(), e.GetTableName()!)))
        {
            // A name from the DbSet property is not the default name and is not cut by the provider.
            var current = group.Key.Item2;
            var first = group.First();
            var isProviderDefault = !group.Any(e => IsChosen(e.GetTableNameConfigurationSource()))
                && string.Equals(current, first.GetDefaultTableName(), StringComparison.Ordinal);
            tables[group.Key] = isProviderDefault ? first.GetDefaultTableName(truncate: false) ?? current : current;
        }

        return tables;
    }

    private static string FullTableName(IConventionEntityType entityType, Dictionary<(string? Schema, string Name), string> tables)
    {
        // An entity type that is mapped to the table of its base type has no entry of its own.
        return tables.TryGetValue((entityType.GetSchema(), entityType.GetTableName()!), out var name) ? name : entityType.GetTableName()!;
    }

    private static string FullColumnName(IConventionProperty property, StoreObjectIdentifier table)
    {
        var root = property.FindSharedStoreObjectRootProperty(table) ?? property;
        var chosen = IsChosen(root.GetColumnNameConfigurationSource()) || IsChosen(root.GetColumnNameConfigurationSource(table));
        return chosen ? root.GetColumnName(table)! : DefaultColumnName(root);
    }

    private static string DefaultColumnName(IConventionProperty property)
    {
        // A property of an owned type that shares the table of its owner is named after the navigation to it as well.
        var name = property.Name;
        var entityType = property.DeclaringType as IConventionEntityType;
        while (entityType?.FindOwnership() is { IsUnique: true, PrincipalToDependent: { } navigation } ownership
            && entityType.GetTableName() == ownership.PrincipalEntityType.GetTableName()
            && entityType.GetSchema() == ownership.PrincipalEntityType.GetSchema())
        {
            name = $"{navigation.Name}_{name}";
            entityType = ownership.PrincipalEntityType;
        }

        return name;
    }

    private static string DefaultKeyName(IConventionKey key, StoreObjectIdentifier table, Dictionary<(string? Schema, string Name), string> tables)
    {
        var name = FullTableName(key.DeclaringEntityType, tables);
        return key.IsPrimaryKey()
            ? $"PK_{name}"
            : $"AK_{name}_{ColumnNames(key.Properties, table)}";
    }

    private static string DefaultForeignKeyName(IConventionForeignKey foreignKey, StoreObjectIdentifier table, Dictionary<(string? Schema, string Name), string> tables)
    {
        return $"FK_{FullTableName(foreignKey.DeclaringEntityType, tables)}_{FullTableName(foreignKey.PrincipalEntityType, tables)}_{ColumnNames(foreignKey.Properties, table)}";
    }

    private static string DefaultIndexName(IConventionIndex index, StoreObjectIdentifier table, Dictionary<(string? Schema, string Name), string> tables)
    {
        return $"IX_{FullTableName(index.DeclaringEntityType, tables)}_{ColumnNames(index.Properties, table)}";
    }

    private static string ColumnNames(IEnumerable<IConventionProperty> properties, StoreObjectIdentifier table)
    {
        return string.Join('_', properties.Select(p => FullColumnName(p, table)));
    }

    private static void ThrowOnCollision(List<Identifier> names)
    {
        foreach (var group in names.GroupBy(n => (n.Space, n.Scope, n.Final)))
        {
            var shortened = group.FirstOrDefault(n => n.Rename is not null);

            // Entity types that map to the same table can declare the same index, which then is one object in the database.
            var other = shortened is null
                ? null
                : group.FirstOrDefault(n => !ReferenceEquals(n, shortened) && !(n.Description == shortened.Description && n.EntityType != shortened.EntityType));
            if (shortened is not null && other is not null)
            {
                throw new InvalidOperationException(
                    $"The {shortened.Description} and the {other.Description} are both named '{group.Key.Final}' in {group.Key.Scope} "
                    + "once the names are shortened for the database. Give one of them a different name.");
            }
        }
    }

    private Identifier Plan(string kind, string space, string scope, string? owner, IConventionEntityType? entityType, string current, string full, bool configured, Action<string> rename)
    {
        // A name that is not chosen is rebuilt from the full names; the current one may carry a suffix of the provider.
        var original = configured ? current : full;
        var shortens = Encoding.UTF8.GetByteCount(original) > _maxLength;
        var name = shortens ? Shorten(original) : current;
        var description = owner is null ? $"{kind} '{original}'" : $"{kind} '{original}' of {owner}";
        return new Identifier(space, scope, description, entityType, name, shortens ? rename : null);
    }

    private string Shorten(string name)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..HashLength];
        var budget = _maxLength - HashLength - 1;

        var length = 0;
        var bytes = 0;
        foreach (var rune in name.EnumerateRunes())
        {
            if (bytes + rune.Utf8SequenceLength > budget)
            {
                break;
            }

            bytes += rune.Utf8SequenceLength;
            length += rune.Utf16SequenceLength;
        }

        return string.Concat(name.AsSpan(0, length), "_", hash);
    }

    private sealed record Identifier(string Space, string Scope, string Description, IConventionEntityType? EntityType, string Final, Action<string>? Rename);
}
