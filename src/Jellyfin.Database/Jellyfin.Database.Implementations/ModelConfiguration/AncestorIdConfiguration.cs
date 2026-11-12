using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// AncestorId configuration.
/// </summary>
public class AncestorIdConfiguration : IEntityTypeConfiguration<AncestorId>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AncestorId> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.ParentItemId });
        // Covers ItemId so descendant lookups by parent never touch the table.
        builder.HasIndex(e => new { e.ParentItemId, e.ItemId });
        builder.HasOne(e => e.ParentItem).WithMany(e => e.Children).HasForeignKey(f => f.ParentItemId);
        builder.HasOne(e => e.Item).WithMany(e => e.Parents).HasForeignKey(f => f.ItemId);
    }
}
