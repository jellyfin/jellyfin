using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// People configuration.
/// </summary>
public class MediaStreamInfoConfiguration : IEntityTypeConfiguration<MediaStreamInfo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MediaStreamInfo> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.StreamIndex });

        // Covering index for the stream filters. ItemId comes second because it is what they project and
        // dedupe on; Language and IsExternal follow only to keep their predicates off the table.
        builder.HasIndex(e => new { e.StreamType, e.ItemId, e.Language, e.IsExternal });
    }
}
