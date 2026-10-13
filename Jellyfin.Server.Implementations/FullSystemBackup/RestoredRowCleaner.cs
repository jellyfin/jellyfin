using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.ModelConfiguration;
using Jellyfin.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Makes the rows of a backup fit the database before they are restored.
/// NUL characters are removed: SQLite stores them, but some engines reject them in text, so a backup that holds them
/// could not be restored there. Keys, roles and item values that are longer than their column are shortened the way
/// they are when they are stored, and provider ids that are longer are dropped.
/// The normalized copies of the original title of an item and of the name of a person are computed again, whatever the backup holds.
/// Cleaning a key or a column of a unique index can make two rows equal. Of such rows the one that needed no cleaning
/// in the columns of that index is kept, because the others are the stale copies the cleaning left behind. When all of
/// them needed cleaning the first in the archive is kept. The rows that referred to a dropped row refer to the kept one.
/// Rows that collide without having been cleaned are not touched, saving them fails as it always did.
/// References from a table to rows of itself are not pointed at the kept row, because the rows that refer to a row
/// may already have been accepted when it is dropped. That would take a table that refers to itself and has a unique
/// index on a string column, which the model does not have, and a test guards that.
/// </summary>
internal sealed class RestoredRowCleaner
{
    /// <summary>
    /// References without a foreign key in the model that still hold the key of a row of another table.
    /// </summary>
    internal static readonly IReadOnlyList<(Type Dependent, string Property, Type Principal)> SoftReferences =
    [
        (typeof(ActivityLog), nameof(ActivityLog.UserId), typeof(User))
    ];

    private static readonly IReadOnlyList<(Type Entity, string Property, Func<string, string> Shorten)> _lengthRules =
    [
        (typeof(UserData), nameof(UserData.CustomDataKey), KeyNormalizer.NormalizeUserDataKey),
        (typeof(Device), nameof(Device.DeviceId), KeyNormalizer.NormalizeDeviceId),
        (typeof(DeviceOptions), nameof(DeviceOptions.DeviceId), KeyNormalizer.NormalizeDeviceId),
        (typeof(PeopleBaseItemMap), nameof(PeopleBaseItemMap.Role), KeyNormalizer.CutPersonRole),
        (typeof(ItemValue), nameof(ItemValue.Value), KeyNormalizer.CutItemValue)
    ];

    private static readonly IReadOnlyList<(Type Entity, string Property, int MaxLength)> _unshortenableBounds =
    [
        (typeof(CustomItemDisplayPreferences), nameof(CustomItemDisplayPreferences.Key), KeyLengths.DisplayPreferencesKey),
        (typeof(BaseItemEntity), nameof(BaseItemEntity.Type), KeyLengths.ItemType),
        (typeof(BaseItemEntity), nameof(BaseItemEntity.MediaType), KeyLengths.MediaType),
        (typeof(Device), nameof(Device.AccessToken), KeyLengths.AccessToken),
        (typeof(ApiKey), nameof(ApiKey.AccessToken), KeyLengths.AccessToken),
        (typeof(User), nameof(User.Username), KeyLengths.Username),
        (typeof(User), nameof(User.NormalizedUsername), KeyLengths.Username),
        (typeof(DisplayPreferences), nameof(DisplayPreferences.Client), KeyLengths.DisplayPreferencesClient),
        (typeof(CustomItemDisplayPreferences), nameof(CustomItemDisplayPreferences.Client), KeyLengths.DisplayPreferencesClient)
    ];

    private readonly ILogger _logger;
    private readonly Dictionary<IEntityType, Dictionary<string, object?[]>> _replacedKeys = [];
    private IEntityType? _table;
    private List<IReadOnlyList<IProperty>> _uniqueKeys = [];
    private List<Dictionary<string, (bool Changed, object?[] PrimaryKey, EntityEntry Entry)>> _seen = [];
    private int _dropped;
    private int _droppedProviderIds;

    public RestoredRowCleaner(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the columns that a restore shortens, drops or refuses a value of when it is longer than the column.
    /// </summary>
    /// <returns>The entity types and the names of the properties.</returns>
    internal static IReadOnlyList<(Type Entity, string Property)> GetRuledColumns()
        => _lengthRules.Select(e => (e.Entity, e.Property))
            .Concat(_unshortenableBounds.Select(e => (e.Entity, e.Property)))
            .Append((typeof(BaseItemProvider), nameof(BaseItemProvider.ProviderId)))
            .ToList();

    /// <summary>
    /// Prepares for the rows of one table. Tables have to be restored after the tables their rows refer to.
    /// </summary>
    /// <param name="table">The table that is restored next.</param>
    public void BeginTable(IEntityType table)
    {
        _table = table;
        _dropped = 0;
        _droppedProviderIds = 0;
        _uniqueKeys = [table.FindPrimaryKey()!.Properties];
        _uniqueKeys.AddRange(table.GetIndexes().Where(i => i.IsUnique && i.GetFilter() is null).Select(i => i.Properties));
        _seen = _uniqueKeys.Select(_ => new Dictionary<string, (bool, object?[], EntityEntry)>(StringComparer.Ordinal)).ToList();
    }

    /// <summary>
    /// Cleans a row of the current table.
    /// </summary>
    /// <param name="entry">The entry of the row.</param>
    /// <returns><c>true</c> if the row is to be restored, <c>false</c> if it is a duplicate of a row that was kept.</returns>
    public bool TryAccept(EntityEntry entry)
    {
        var table = _table!;
        var changed = new HashSet<IProperty>();
        foreach (var property in table.GetProperties().Where(e => e.ClrType == typeof(string)))
        {
            var propertyEntry = entry.Property(property.Name);
            if (propertyEntry.CurrentValue is string text)
            {
                var cleaned = text.RemoveNullCharacters();
                if (cleaned.Length != text.Length)
                {
                    propertyEntry.CurrentValue = cleaned;
                    changed.Add(property);
                }
            }
        }

        // A backup made before these columns existed holds nothing in them, and one made since holds what the
        // application computed then, so they are always computed again.
        switch (entry.Entity)
        {
            case BaseItemEntity item:
                item.CleanOriginalTitle = MatchNormalizer.CleanOriginalTitle(item.OriginalTitle);
                break;
            case People person:
                person.NameLower = MatchNormalizer.NameLower(person.Name);
                break;
        }

        if (entry.Entity is BaseItemProvider provider && provider.ProviderId.Length > KeyNormalizer.ProviderIdMaxLength)
        {
            _droppedProviderIds++;
            return false;
        }

        foreach (var (_, ruleProperty, shorten) in _lengthRules.Where(e => e.Entity == table.ClrType))
        {
            var propertyEntry = entry.Property(ruleProperty);
            if (propertyEntry.CurrentValue is string text && shorten(text) is var shortened && !string.Equals(shortened, text, StringComparison.Ordinal))
            {
                propertyEntry.CurrentValue = shortened;
                changed.Add(propertyEntry.Metadata);
                if (entry.Entity is ItemValue itemValue)
                {
                    itemValue.CleanValue = shortened.GetCleanValue();
                }
            }
        }

        foreach (var foreignKey in table.GetForeignKeys())
        {
            if (!foreignKey.PrincipalKey.IsPrimaryKey() || !_replacedKeys.TryGetValue(foreignKey.PrincipalEntityType, out var replaced))
            {
                continue;
            }

            var values = foreignKey.Properties.Select(e => entry.Property(e.Name).CurrentValue).ToArray();
            if (Identify(values) is { } id && replaced.TryGetValue(id, out var replacement))
            {
                for (var i = 0; i < replacement.Length; i++)
                {
                    entry.Property(foreignKey.Properties[i].Name).CurrentValue = replacement[i];
                    changed.Add(foreignKey.Properties[i]);
                }
            }
        }

        foreach (var (_, property, principalType) in SoftReferences.Where(e => e.Dependent == table.ClrType))
        {
            var principal = table.Model.FindEntityType(principalType)!;
            var propertyEntry = entry.Property(property);
            if (_replacedKeys.TryGetValue(principal, out var replaced)
                && Identify([propertyEntry.CurrentValue]) is { } id
                && replaced.TryGetValue(id, out var replacement))
            {
                propertyEntry.CurrentValue = replacement[0];
            }
        }

        var primaryKey = table.FindPrimaryKey()!.Properties.Select(e => entry.Property(e.Name).CurrentValue).ToArray();
        var identities = _uniqueKeys.Select(key => Identify(key.Select(e => entry.Property(e.Name).CurrentValue).ToArray())).ToList();
        var collisions = new List<(int Key, (bool Changed, object?[] PrimaryKey, EntityEntry Entry) Existing)>();
        for (var i = 0; i < _uniqueKeys.Count; i++)
        {
            if (identities[i] is { } id
                && _seen[i].TryGetValue(id, out var existing)
                && (existing.Changed || _uniqueKeys[i].Any(changed.Contains)))
            {
                collisions.Add((i, existing));
            }
        }

        if (collisions.Count > 0)
        {
            // The row that was cleaned loses against one that needed no cleaning in the columns that collide.
            if (!collisions.All(e => e.Existing.Changed && !_uniqueKeys[e.Key].Any(changed.Contains)))
            {
                Drop(table, _uniqueKeys[collisions[0].Key], primaryKey, collisions[0].Existing.PrimaryKey);
                return false;
            }

            foreach (var evicted in collisions.Select(e => e.Existing).DistinctBy(e => e.Entry))
            {
                Evict(table, _uniqueKeys[collisions[0].Key], evicted, primaryKey);
            }
        }

        for (var i = 0; i < _uniqueKeys.Count; i++)
        {
            if (identities[i] is { } id)
            {
                _seen[i].TryAdd(id, (_uniqueKeys[i].Any(changed.Contains), primaryKey, entry));
            }
        }

        return true;
    }

    /// <summary>
    /// Finishes the current table.
    /// </summary>
    public void EndTable()
    {
        if (_dropped > 0)
        {
            _logger.LogInformation(
                "Dropped {Count} rows of {Table} that are duplicates of other rows once they are cleaned",
                _dropped,
                _table!.ClrType.Name);
        }

        if (_droppedProviderIds > 0)
        {
            _logger.LogWarning(
                "Dropped {Count} provider ids of {Table} that are longer than the {MaxLength} characters that are stored",
                _droppedProviderIds,
                _table!.ClrType.Name,
                KeyNormalizer.ProviderIdMaxLength);
        }
    }

    /// <summary>
    /// Fails for a row that holds a value which is longer than its column and which the restore does not shorten.
    /// </summary>
    /// <param name="table">The table of the row.</param>
    /// <param name="entity">The row.</param>
    /// <exception cref="InvalidOperationException">The value is longer than its column.</exception>
    public static void ThrowIfValueCannotBeShortened(IEntityType table, object entity)
    {
        foreach (var (_, property, maxLength) in _unshortenableBounds.Where(e => e.Entity == table.ClrType))
        {
            var value = (table.FindProperty(property)!.GetGetter().GetClrValue(entity) as string)?.RemoveNullCharacters();
            if (value is not null && value.Length > maxLength)
            {
                throw new InvalidOperationException(
                    $"The backup cannot be restored: column {property} of table {table.GetTableName()} holds a value of {value.Length.ToString(CultureInfo.InvariantCulture)} characters and the longest that can be stored is {maxLength.ToString(CultureInfo.InvariantCulture)}.");
            }
        }
    }

    // Values that are null never collide in a unique index.
    private static string? Identify(object?[] values)
    {
        if (values.Any(e => e is null))
        {
            return null;
        }

        return string.Join(',', values.Select(e =>
        {
            var text = Convert.ToString(e, CultureInfo.InvariantCulture)!;
            return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
        }));
    }

    private void Drop(IEntityType table, IReadOnlyList<IProperty> key, object?[] primaryKey, object?[] keptPrimaryKey)
    {
        _dropped++;
        _logger.LogDebug(
            "Dropped a row of {Table} whose columns {Columns} duplicate those of another row once it is cleaned",
            table.ClrType.Name,
            string.Join(", ", key.Select(e => e.Name)));

        Replace(table, primaryKey, keptPrimaryKey);
    }

    // The row that was accepted before is taken back: it is detached again, forgotten, and its key now stands for
    // the new row, as do the keys of the rows that were dropped in favour of it.
    private void Evict(IEntityType table, IReadOnlyList<IProperty> key, (bool Changed, object?[] PrimaryKey, EntityEntry Entry) evicted, object?[] keptPrimaryKey)
    {
        evicted.Entry.State = EntityState.Detached;
        foreach (var seen in _seen)
        {
            foreach (var stale in seen.Where(e => e.Value.Entry == evicted.Entry).Select(e => e.Key).ToList())
            {
                seen.Remove(stale);
            }
        }

        _dropped++;
        _logger.LogDebug(
            "Dropped a row of {Table} whose columns {Columns} duplicate those of another row once it is cleaned",
            table.ClrType.Name,
            string.Join(", ", key.Select(e => e.Name)));

        var evictedId = Identify(evicted.PrimaryKey);
        if (_replacedKeys.TryGetValue(table, out var replaced) && evictedId is not null)
        {
            foreach (var earlier in replaced.Where(e => Identify(e.Value) == evictedId).Select(e => e.Key).ToList())
            {
                replaced[earlier] = keptPrimaryKey;
            }
        }

        Replace(table, evicted.PrimaryKey, keptPrimaryKey);
    }

    private void Replace(IEntityType table, object?[] primaryKey, object?[] keptPrimaryKey)
    {
        var id = Identify(primaryKey);
        if (id is not null && id != Identify(keptPrimaryKey))
        {
            if (!_replacedKeys.TryGetValue(table, out var replaced))
            {
                replaced = new Dictionary<string, object?[]>(StringComparer.Ordinal);
                _replacedKeys[table] = replaced;
            }

            replaced[id] = keptPrimaryKey;
        }
    }
}
