using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Gives every column that the server stores as text the collation of <see cref="MySqlDatabaseProvider.Collation"/>.
/// </summary>
/// <remarks>
/// <para>
/// A column is chosen by the type it has in the database, once the model is complete, not by the type of its property.
/// A property of another type can be stored as text: a list of numbers is stored as a JSON array in a <c>longtext</c>
/// column, and a value converter can turn any type into a string. A choice by the type of the property would leave
/// such a column to the collation the server picks, which is a case-insensitive one.
/// </para>
/// <para>
/// The server does not fall back to the collation of the table or of the database: a column that names its character
/// set and no collation gets the default collation of that character set, and the provider names the character set of
/// every text column. So each text column names the collation itself.
/// </para>
/// <para>
/// A <c>json</c> column of MySQL is not text: it has the binary character set and compares as JSON, and it takes no
/// collation. MariaDB stores <c>json</c> as <c>longtext</c> with the collation <c>utf8mb4_bin</c>. The model has no such
/// column; one would need its own decision.
/// </para>
/// </remarks>
internal sealed class TextCollationConvention : IModelFinalizingConvention
{
    // The character types of both servers that take a collation. The national types are left out: they use a character
    // set of three bytes, which this collation is not valid for.
    private static readonly HashSet<string> _textTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "char",
        "varchar",
        "tinytext",
        "text",
        "mediumtext",
        "longtext"
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
    /// <param name="storeType">The type of the column, for example <c>varchar(256)</c>.</param>
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
                    property.Builder.UseCollation(MySqlDatabaseProvider.Collation);
                }
            }
        }
    }
}
