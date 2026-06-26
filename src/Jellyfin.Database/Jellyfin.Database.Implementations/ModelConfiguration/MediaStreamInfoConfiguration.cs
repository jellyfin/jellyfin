using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// MediaStreamInfo Configuration.
/// </summary>
public class MediaStreamInfoConfiguration : IEntityTypeConfiguration<MediaStreamInfo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MediaStreamInfo> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.StreamIndex });
    }
}
