using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the BaseItemImageInfo entity.
/// </summary>
public class BaseItemImageInfoConfiguration : IEntityTypeConfiguration<BaseItemImageInfo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemImageInfo> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Item).WithMany(e => e.Images).HasForeignKey(e => e.ItemId);

        // Composite index for filtering by item and image type (also covers ItemId-only lookups)
        builder.HasIndex(e => new { e.ItemId, e.ImageType });
    }
}
