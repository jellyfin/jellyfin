using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// Configuration for BaseItemImageInfo.
/// </summary>
public class BaseItemImageInfoConfiguration : IEntityTypeConfiguration<BaseItemImageInfo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemImageInfo> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Item).WithMany(e => e.Images).HasForeignKey(e => e.ItemId);

        // 3-column index for efficient image ordering queries
        // SortOrder reflects LocalImageProvider discovery order per image type
        builder.HasIndex(e => new { e.ItemId, e.ImageType, e.SortOrder });
    }
}
