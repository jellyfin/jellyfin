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
        builder.HasIndex(e => e.ParentItemId);
        builder.HasOne(e => e.ParentItem).WithMany(e => e.Children).HasForeignKey(f => f.ParentItemId);
        builder.HasOne(e => e.Item).WithMany(e => e.Parents).HasForeignKey(f => f.ItemId);
    }
}
