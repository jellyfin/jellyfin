using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Builds a valid row for any table of the model, whatever the model holds, so that a row of the kit covers a table
/// that is added later without being changed. Every property gets a value. The rows that a required relationship
/// points at are saved first, in a context of their own: one for every row when the relationship is part of a key or
/// a unique index, which would otherwise allow only one row, and one shared by every row otherwise.
/// </summary>
/// <remarks>
/// Every string of a row gets the same value, a lowercase token of hex digits, cut to the maximum length of its
/// column. So a column that the server derives from another one, such as a lowercased name, holds what the server
/// would derive, and the row reads back the same after a restore computes it again.
/// </remarks>
public sealed class ModelRowFactory
{
    /// <summary>
    /// The value of every date of a row. It has no fraction of a second, so no engine has to round it.
    /// </summary>
    public static readonly DateTime StoredDate = new(2021, 7, 15, 12, 34, 56, DateTimeKind.Utc);

    private static readonly MethodInfo _entitiesOf = typeof(ModelRowFactory).GetMethod(nameof(EntitiesOf), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly Func<JellyfinDbContext> _createContext;
    private readonly Dictionary<string, object?[]> _principals = new(StringComparer.Ordinal);
    private readonly HashSet<string> _creating = new(StringComparer.Ordinal);
    private int _rows;

    public ModelRowFactory(Func<JellyfinDbContext> createContext)
    {
        _createContext = createContext;
    }

    /// <summary>
    /// Gets one entity type for every table of the model, in a fixed order.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The entity types.</returns>
    public static IReadOnlyList<IEntityType> Tables(IModel model)
    {
        return model.GetEntityTypes()
            .Where(entityType => entityType.GetTableName() is not null)
            .GroupBy(entityType => entityType.GetTableName(), StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(entityType => entityType.GetTableName(), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Gets the key column whose values the engine generates, if the table has one.
    /// </summary>
    /// <param name="entityType">The entity type of the table.</param>
    /// <returns>The property of the column, or <see langword="null"/>.</returns>
    public static IProperty? IdentityKey(IEntityType entityType)
    {
        return entityType.FindPrimaryKey()?.Properties is [{ ValueGenerated: ValueGenerated.OnAdd } key]
            && (key.ClrType == typeof(int) || key.ClrType == typeof(long))
                ? key
                : null;
    }

    /// <summary>
    /// Describes every row of a table, one line per row in ordinal order, from all of its columns.
    /// </summary>
    /// <param name="context">The context to read with.</param>
    /// <param name="entityType">The entity type of the table.</param>
    /// <param name="include">Which columns to describe; all of them by default.</param>
    /// <returns>The descriptions.</returns>
    public static IReadOnlyList<string> DescribeRows(JellyfinDbContext context, IEntityType entityType, Func<IProperty, bool>? include = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entities = (IEnumerable<object>)_entitiesOf.MakeGenericMethod(entityType.ClrType).Invoke(null, [context])!;
        return entities.Select(entity => Describe(context.Entry(entity), include)).Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Reads the primary keys of every row of a table.
    /// </summary>
    /// <param name="context">The context to read with.</param>
    /// <param name="entityType">The entity type of the table.</param>
    /// <returns>The values of the key of each row, in the order of the key.</returns>
    public static IReadOnlyList<object?[]> Keys(JellyfinDbContext context, IEntityType entityType)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entities = (IEnumerable<object>)_entitiesOf.MakeGenericMethod(entityType.ClrType).Invoke(null, [context])!;
        return entities.Select(entity => KeyOf(context.Entry(entity))).ToList();
    }

    /// <summary>
    /// Describes one row from all of its columns.
    /// </summary>
    /// <param name="entry">The entry of the row.</param>
    /// <param name="include">Which columns to describe; all of them by default.</param>
    /// <returns>The description.</returns>
    public static string Describe(EntityEntry entry, Func<IProperty, bool>? include = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return string.Join(
            "; ",
            entry.Metadata.GetProperties()
                .Where(include ?? (_ => true))
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => $"{property.Name}={Format(entry.Property(property.Name).CurrentValue)}"));
    }

    /// <summary>
    /// Reads a row back by its key, in a context of its own.
    /// </summary>
    /// <param name="context">The context to read with.</param>
    /// <param name="entityType">The entity type of the table.</param>
    /// <param name="key">The values of the primary key.</param>
    /// <returns>The entry of the row, or <see langword="null"/> when there is none.</returns>
    public static EntityEntry? Find(JellyfinDbContext context, IEntityType entityType, object?[] key)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = context.Find(entityType.ClrType, key);
        return entity is null ? null : context.Entry(entity);
    }

    /// <summary>
    /// Gets the values of the primary key of a row.
    /// </summary>
    /// <param name="entry">The entry of the row.</param>
    /// <returns>The values, in the order of the key.</returns>
    public static object?[] KeyOf(EntityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.Metadata.FindPrimaryKey()!.Properties.Select(property => entry.Property(property.Name).CurrentValue).ToArray();
    }

    /// <summary>
    /// Adds a row to the context without saving it. The rows its required relationships point at are saved first,
    /// in contexts of their own, unless <paramref name="values"/> has their foreign keys; see <see cref="PrincipalValues"/>.
    /// </summary>
    /// <param name="context">The context that saves the row.</param>
    /// <param name="entityType">The entity type of the table.</param>
    /// <param name="values">Values that replace the generated ones, by property name.</param>
    /// <returns>The entry of the row, in the state <see cref="EntityState.Added"/>.</returns>
    public EntityEntry Add(JellyfinDbContext context, IEntityType entityType, IReadOnlyDictionary<string, object?>? values = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entityType);
        var table = context.Model.FindEntityType(entityType.Name)!;
        var entry = context.Entry(RuntimeHelpers.GetUninitializedObject(table.ClrType));
        var token = Guid.NewGuid().ToString("N");
        _rows++;

        var foreignKeyProperties = table.GetForeignKeys().SelectMany(foreignKey => foreignKey.Properties).ToHashSet();
        foreach (var (name, value) in PrincipalValues(table, values))
        {
            entry.Property(name).CurrentValue = value;
        }

        foreach (var property in table.GetProperties())
        {
            // The engine generates the value of an identity column; any other generated value is set here.
            if (foreignKeyProperties.Contains(property) || (property.ValueGenerated != ValueGenerated.Never && property.ClrType != typeof(Guid)))
            {
                continue;
            }

            entry.Property(property.Name).CurrentValue = CreateValue(property, token);
        }

        foreach (var (name, value) in values ?? new Dictionary<string, object?>())
        {
            entry.Property(name).CurrentValue = value;
        }

        entry.State = EntityState.Added;
        return entry;
    }

    /// <summary>
    /// Adds a row in a context of its own and saves it.
    /// </summary>
    /// <param name="entityType">The entity type of the table.</param>
    /// <param name="values">Values that replace the generated ones, by property name.</param>
    /// <returns>The description of the row as it was saved, and its key.</returns>
    public (string Row, object?[] Key) Save(IEntityType entityType, IReadOnlyDictionary<string, object?>? values = null)
    {
        using var context = _createContext();
        var entry = Add(context, entityType, values);
        context.SaveChanges();
        return (Describe(entry), KeyOf(entry));
    }

    /// <summary>
    /// Saves the rows that the required relationships of a new row of a table point at, and gives back the values of
    /// its foreign keys. Pass them to <see cref="Add"/> to add a row on a context that is in a transaction, which a
    /// context of its own may not be able to write next to on an engine whose contexts share one connection.
    /// </summary>
    /// <param name="entityType">The entity type of the table.</param>
    /// <param name="values">Values the row already has, by property name; relationships they cover are left out.</param>
    /// <returns>The values of the foreign keys, by property name.</returns>
    public Dictionary<string, object?> PrincipalValues(IEntityType entityType, IReadOnlyDictionary<string, object?>? values = null)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var foreignKey in entityType.GetForeignKeys())
        {
            if (!IsRequired(foreignKey) || foreignKey.Properties.All(property => values?.ContainsKey(property.Name) == true))
            {
                continue;
            }

            if (!foreignKey.PrincipalKey.IsPrimaryKey())
            {
                throw new NotSupportedException($"{entityType.DisplayName()} refers to an alternate key of {foreignKey.PrincipalEntityType.DisplayName()}.");
            }

            var principal = IsUnique(foreignKey) ? Save(foreignKey.PrincipalEntityType).Key : Principal(foreignKey.PrincipalEntityType);
            for (var i = 0; i < foreignKey.Properties.Count; i++)
            {
                result[foreignKey.Properties[i].Name] = principal[i];
            }
        }

        return result;
    }

    private static bool IsRequired(IForeignKey foreignKey)
        => foreignKey.IsRequired || foreignKey.Properties.Any(property => !property.IsNullable);

    private static bool IsUnique(IForeignKey foreignKey)
    {
        var dependent = foreignKey.DeclaringEntityType;
        return dependent.GetKeys().Select(key => key.Properties)
            .Concat(dependent.GetIndexes().Where(index => index.IsUnique).Select(index => index.Properties))
            .Any(properties => properties.Intersect(foreignKey.Properties).Any());
    }

    private static List<object> EntitiesOf<TEntity>(JellyfinDbContext context)
        where TEntity : class
        => context.Set<TEntity>().AsNoTracking().Cast<object>().ToList();

    private static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            DateTime date => $"{date.Ticks.ToString(CultureInfo.InvariantCulture)}/{date.Kind}",
            byte[] bytes => Convert.ToHexString(bytes),
            string text => text,
            IEnumerable items => "[" + string.Join(",", items.Cast<object?>().Select(Format)) + "]",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private object?[] Principal(IEntityType entityType)
    {
        if (_principals.TryGetValue(entityType.Name, out var key))
        {
            return key;
        }

        if (!_creating.Add(entityType.Name))
        {
            throw new NotSupportedException($"The required relationships of {entityType.DisplayName()} form a cycle.");
        }

        key = Save(entityType).Key;
        _creating.Remove(entityType.Name);
        _principals[entityType.Name] = key;
        return key;
    }

    private object? CreateValue(IProperty property, string token)
    {
        if (property.IsPrimitiveCollection)
        {
            var elementType = property.GetElementType()!.ClrType;
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
            list.Add(Convert.ChangeType(1, elementType, CultureInfo.InvariantCulture));
            list.Add(Convert.ChangeType(2, elementType, CultureInfo.InvariantCulture));
            return list;
        }

        var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        if (type == typeof(string))
        {
            return property.GetMaxLength() is int maxLength && maxLength < token.Length ? token[..maxLength] : token;
        }

        if (type == typeof(Guid))
        {
            return Guid.NewGuid();
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(DateTime))
        {
            return StoredDate;
        }

        if (type == typeof(byte[]))
        {
            return new byte[] { 1, 2, 3 };
        }

        if (type.IsEnum)
        {
            var defined = Enum.GetValues(type);
            return defined.GetValue(defined.Length - 1);
        }

        if (type.IsPrimitive || type == typeof(decimal))
        {
            // Different for each row, so that two rows of one parent do not share a composite key.
            return Convert.ChangeType(1 + (_rows % 100), type, CultureInfo.InvariantCulture);
        }

        throw new NotSupportedException($"The kit has no value for {property.DeclaringType.DisplayName()}.{property.Name} of type {type}.");
    }
}
