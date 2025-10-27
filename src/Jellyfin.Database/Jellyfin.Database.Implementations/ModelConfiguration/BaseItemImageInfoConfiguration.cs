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

        // Index on ImageType and SortOrder for efficient ordering queries
        // ImageType is required because SortOrder is per-type (each type starts from 0)
        builder.HasIndex(e => new { e.ItemId, e.ImageType, e.SortOrder });
    }
}
