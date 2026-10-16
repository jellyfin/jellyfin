using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Creates the foreign keys of an item to its parent, and of an ancestor row to its ancestor, without an action on
/// delete, because SQL Server cannot create them as cascades.
/// </summary>
/// <remarks>
/// SQL Server refuses a cascade from a table to itself, which the parent of an item is, and a second cascade path to a
/// table, which an ancestor row gets through both of its items. The server deletes the children and ancestor rows of a
/// deleted item itself, before the item, so the rows the cascades would delete are gone already.
/// </remarks>
internal sealed class CascadePathConvention : IModelFinalizingConvention
{
    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var foreignKey in modelBuilder.Metadata.GetEntityTypes().SelectMany(e => e.GetDeclaredForeignKeys()))
        {
            if (IsParent(foreignKey) || IsAncestor(foreignKey))
            {
                ((IMutableForeignKey)foreignKey).DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }

    private static bool IsParent(IConventionForeignKey foreignKey)
        => foreignKey.DeclaringEntityType.ClrType == typeof(BaseItemEntity)
            && foreignKey.Properties is [{ Name: nameof(BaseItemEntity.ParentId) }];

    private static bool IsAncestor(IConventionForeignKey foreignKey)
        => foreignKey.DeclaringEntityType.ClrType == typeof(AncestorId)
            && foreignKey.Properties is [{ Name: nameof(AncestorId.ParentItemId) }];
}
