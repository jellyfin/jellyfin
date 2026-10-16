using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Gives every column that the server stores as text the collation of <see cref="SqlServerDatabaseProvider.Collation"/>.
/// </summary>
/// <remarks>
/// <para>
/// A column without a collation of its own takes the default collation of the database, which is the one the operator
/// created it with, and the default of the server is case-insensitive. So each text column names the collation itself,
/// whatever the database was created with, and the migrations do not change the default of the database, which takes
/// exclusive access to it.
/// </para>
/// <para>
/// A column is chosen by the type it has in the database, once the model is complete, not by the type of its property.
/// A property of another type can be stored as text: a list of values is stored as JSON in an <c>nvarchar(max)</c>
/// column, and a value converter can turn any type into a string.
/// </para>
/// </remarks>
internal sealed class TextCollationConvention : IModelFinalizingConvention
{
    // The character types of the server, all of which take a collation.
    private static readonly HashSet<string> _textTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "char",
        "varchar",
        "text",
        "nchar",
        "nvarchar",
        "ntext"
    };

    private readonly IRelationalTypeMappingSource _typeMappingSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextCollationConvention"/> class.
    /// </summary>
    /// <param name="typeMappingSource">The source of the database types of the provider.</param>
    public TextCollationConvention(IRelationalTypeMappingSource typeMappingSource)
    {
        _typeMappingSource = typeMappingSource;
    }

    /// <summary>
    /// Gets whether a type of the database is one of the text types that this convention gives the collation.
    /// </summary>
    /// <param name="storeType">The type of the column, for example <c>nvarchar(4000)</c>.</param>
    /// <returns>Whether the column stores text.</returns>
    internal static bool IsText(string? storeType)
    {
        if (string.IsNullOrWhiteSpace(storeType))
        {
            return false;
        }

        var end = storeType.AsSpan().IndexOfAny('(', ' ');
        return _textTypes.Contains(end < 0 ? storeType.Trim() : storeType[..end]);
    }

    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                // A type that the model names is used as it is; otherwise the type comes from the provider, as the type
                // mapping of the property is only set after this convention.
                var storeType = property.GetColumnType() ?? _typeMappingSource.FindMapping((IProperty)property)?.StoreType;
                if (IsText(storeType))
                {
                    property.Builder.UseCollation(SqlServerDatabaseProvider.Collation);
                }
            }
        }
    }
}
