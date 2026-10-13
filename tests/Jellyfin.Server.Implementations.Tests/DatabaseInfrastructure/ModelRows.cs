using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Puts rows into every table of the model, whatever the model holds, so that a test notices a table that a reset
/// forgets, and a table that the test itself does not know about yet.
/// </summary>
internal static class ModelRows
{
    public static IReadOnlyList<IEntityType> Tables(JellyfinDbContext context)
    {
        return context.Model.GetEntityTypes()
            .Where(entityType => entityType.GetTableName() is not null)
            .GroupBy(entityType => entityType.GetTableName())
            .Select(group => group.First())
            .ToList();
    }

    public static IReadOnlyList<IEntityType> TablesWithIdentity(JellyfinDbContext context)
    {
        return Tables(context).Where(entityType => GetIdentityKey(entityType) is not null).ToList();
    }

    public static IProperty? GetIdentityKey(IEntityType entityType)
    {
        return entityType.FindPrimaryKey()?.Properties is [{ ValueGenerated: ValueGenerated.OnAdd } key]
            && (key.ClrType == typeof(int) || key.ClrType == typeof(long))
                ? key
                : null;
    }

    /// <summary>
    /// Inserts one row into every table, and several into those that count their rows with an identity column.
    /// </summary>
    /// <param name="context">The context of the database.</param>
    /// <param name="identityRows">The number of rows for a table with an identity column.</param>
    /// <returns>The inserted entities, by the entity type they were made for.</returns>
    public static IReadOnlyList<(IEntityType EntityType, EntityEntry Entry)> Fill(JellyfinDbContext context, int identityRows)
    {
        var added = new List<(IEntityType, EntityEntry)>();
        foreach (var entityType in Tables(context))
        {
            var rows = GetIdentityKey(entityType) is null ? 1 : identityRows;
            for (var row = 0; row < rows; row++)
            {
                var entry = context.Entry(RuntimeHelpers.GetUninitializedObject(entityType.ClrType));
                foreach (var property in entityType.GetProperties().Where(property => property.ValueGenerated == ValueGenerated.Never))
                {
                    var value = CreateValue(Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType);
                    if (value is not null)
                    {
                        entry.Property(property.Name).CurrentValue = value;
                    }
                }

                entry.State = EntityState.Added;
                added.Add((entityType, entry));
            }
        }

        // The rows only have to exist, so they do not have to point at each other.
        context.Database.OpenConnection();
        context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        context.SaveChanges();
        return added;
    }

    private static object? CreateValue(Type type)
    {
        if (type == typeof(string))
        {
            return Guid.NewGuid().ToString("N");
        }

        if (type == typeof(Guid))
        {
            return Guid.NewGuid();
        }

        return type == typeof(byte[]) ? Guid.NewGuid().ToByteArray() : null;
    }
}
