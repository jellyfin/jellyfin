using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// BaseItemImageInfo Configuration.
/// </summary>
public class BaseItemImageInfoConfiguration : IEntityTypeConfiguration<BaseItemImageInfo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemImageInfo> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ItemId); // Index for queries filtering by ItemId
        builder.HasOne(e => e.Item).WithMany(e => e.Images).HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Cascade); // Delete image info when item is deleted
    }
}
