using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the <see cref="MediaSegment"/> entity.
/// </summary>
public class MediaSegmentConfiguration : IEntityTypeConfiguration<MediaSegment>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MediaSegment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.ItemId, e.StartTicks });
    }
}
