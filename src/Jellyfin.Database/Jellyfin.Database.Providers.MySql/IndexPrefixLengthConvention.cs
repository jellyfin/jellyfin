using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.ModelConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microting.EntityFrameworkCore.MySql.Metadata;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Stores the indexed text without a length bound as <c>longtext</c> and indexes only the first characters of it, so
/// that every index fits the size an index entry may have.
/// </summary>
/// <remarks>
/// <para>
/// Without a type, the provider would give an indexed string without a length the type <c>varchar(255)</c> and cut
/// longer values off or reject them. An index over a <c>longtext</c> column needs a prefix. Each such column of an
/// index gets the same number of characters, as many as fit in what is left of the size once the other columns of the
/// index are counted at their full width, but at most <see cref="MaxPrefixLength"/>. A string that has a length bound
/// keeps its type and is indexed in full.
/// </para>
/// <para>
/// A prefix index can find the rows of a value but cannot tell whether a value is unique. A key or a unique index over
/// such a column is therefore rejected: MariaDB would turn it into a hash that quietly allows duplicates that differ
/// only after the prefix, and MySQL would refuse to create it.
/// </para>
/// </remarks>
internal sealed class IndexPrefixLengthConvention : IModelFinalizingConvention
{
    /// <summary>
    /// The size an index entry may have, in bytes, on InnoDB with the default row format.
    /// </summary>
    internal const int MaxKeyBytes = 3072;

    /// <summary>
    /// The most bytes a character takes in a <c>utf8mb4</c> column.
    /// </summary>
    internal const int BytesPerChar = 4;

    /// <summary>
    /// The longest prefix of an unbounded column that is indexed, in characters.
    /// </summary>
    internal const int MaxPrefixLength = 512;

    /// <summary>
    /// The column type of indexed text without a length bound.
    /// </summary>
    internal const string UnboundedColumnType = "longtext";

    // The widths of the types that can be part of an index, in the units the server stores them in. The values are
    // the largest of the engines the model is checked against.
    private static readonly Dictionary<Type, int> _fixedSizes = new()
    {
        [typeof(bool)] = 1,
        [typeof(byte)] = 1,
        [typeof(sbyte)] = 1,
        [typeof(short)] = 2,
        [typeof(ushort)] = 2,
        [typeof(int)] = 4,
        [typeof(uint)] = 4,
        [typeof(float)] = 4,
        [typeof(DateOnly)] = 4,
        [typeof(long)] = 8,
        [typeof(ulong)] = 8,
        [typeof(double)] = 8,
        [typeof(DateTime)] = 8,
        [typeof(TimeSpan)] = 8,
        [typeof(TimeOnly)] = 8,
        [typeof(DateTimeOffset)] = 10,
        [typeof(Guid)] = 16,
        [typeof(decimal)] = 17
    };

    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties().Where(p => p.IsUnboundedIndexedText()))
            {
                property.SetColumnType(UnboundedColumnType);
            }

            if (entityType.GetDeclaredKeys().FirstOrDefault(k => k.Properties.Any(p => p.IsUnboundedIndexedText())) is { } key)
            {
                throw new InvalidOperationException(
                    $"The key {Describe(entityType, key.Properties)} holds text without a length bound, which cannot be a key on MySQL and MariaDB. Give the text a maximum length.");
            }

            foreach (var index in entityType.GetDeclaredIndexes())
            {
                var prefix = Plan(entityType, index);
                if (prefix is not null)
                {
                    index.SetPrefixLength(prefix);
                }
            }
        }
    }

    private static int[]? Plan(IConventionEntityType entityType, IConventionIndex index)
    {
        var unbounded = index.Properties.Count(p => p.IsUnboundedIndexedText());
        if (unbounded == 0)
        {
            return null;
        }

        if (index.IsUnique)
        {
            throw new InvalidOperationException(
                $"The unique index {Describe(entityType, index.Properties)} holds text without a length bound. Only a prefix of it could be indexed, which cannot guarantee that the values are unique.");
        }

        var otherBytes = index.Properties.Where(p => !p.IsUnboundedIndexedText()).Sum(SizeOf);
        var characters = Math.Min(MaxPrefixLength, (MaxKeyBytes - otherBytes) / BytesPerChar / unbounded);
        if (characters < 1)
        {
            throw new InvalidOperationException(
                $"The index {Describe(entityType, index.Properties)} has no room left for the text without a length bound, as the other columns take {otherBytes} of the {MaxKeyBytes} bytes.");
        }

        return index.Properties.Select(p => p.IsUnboundedIndexedText() ? characters : 0).ToArray();
    }

    private static int SizeOf(IConventionProperty property)
    {
        var type = property.GetProviderClrType() ?? property.ClrType;
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string))
        {
            return (property.GetMaxLength() ?? throw new InvalidOperationException($"The indexed text {property.DeclaringType.DisplayName()}.{property.Name} has neither a maximum length nor a mark as text without one.")) * BytesPerChar;
        }

        type = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
        return _fixedSizes.TryGetValue(type, out var size)
            ? size
            : throw new InvalidOperationException($"The size of the indexed column {property.DeclaringType.DisplayName()}.{property.Name} of type {type} is not known.");
    }

    private static string Describe(IConventionEntityType entityType, IEnumerable<IConventionProperty> properties)
    {
        return $"{entityType.DisplayName()} ({string.Join(", ", properties.Select(p => p.Name))})";
    }
}
