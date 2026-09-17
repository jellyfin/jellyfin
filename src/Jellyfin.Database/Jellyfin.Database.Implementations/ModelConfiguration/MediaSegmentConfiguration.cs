using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the MediaSegment entity.
/// </summary>
public class MediaSegmentConfiguration : IEntityTypeConfiguration<MediaSegment>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MediaSegment> builder)
    {
        builder.HasOne<BaseItemEntity>()
            .WithMany()
            .HasForeignKey(segment => segment.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
