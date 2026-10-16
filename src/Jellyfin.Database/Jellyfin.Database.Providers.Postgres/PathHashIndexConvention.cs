using System;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Jellyfin.Database.Providers.Postgres;

/// <summary>
/// Makes the index on the path of an item a hash index.
/// </summary>
/// <remarks>
/// A btree index entry may hold at most 2704 bytes, so a btree index on the path refuses an item whose path does not
/// compress to that size, while SQLite saves it. A hash index stores a 4-byte hash of the value instead, whatever its
/// length. It answers only equality and membership (<c>= ANY</c>), which are the only ways the path is looked up. It
/// cannot return rows in order, so a lookup that orders by the path sorts the rows it found, and it holds no nulls,
/// which no lookup by path asks for. The convention fails when the model has no index on the path, so that a change to
/// the shared index cannot leave PostgreSQL with a different one.
/// </remarks>
internal sealed class PathHashIndexConvention : IModelFinalizingConvention
{
    /// <summary>
    /// The PostgreSQL access method of the index.
    /// </summary>
    internal const string Method = "hash";

    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var entityType = modelBuilder.Metadata.FindEntityType(typeof(BaseItemEntity));
        var path = entityType?.FindProperty(nameof(BaseItemEntity.Path));
        var index = path is null ? null : entityType!.FindIndex(path);
        if (index is null)
        {
            throw new InvalidOperationException(
                $"The model has no index on {nameof(BaseItemEntity)}.{nameof(BaseItemEntity.Path)} alone, which the PostgreSQL provider makes a hash index.");
        }

        // PostgreSQL cannot make a hash index unique. A filter it would accept; PostgresPathIndexTests pins that there is none.
        if (index.IsUnique)
        {
            throw new InvalidOperationException(
                $"The index on {nameof(BaseItemEntity)}.{nameof(BaseItemEntity.Path)} is unique, which a PostgreSQL hash index cannot be.");
        }

        index.Builder.HasMethod(Method);
    }
}
