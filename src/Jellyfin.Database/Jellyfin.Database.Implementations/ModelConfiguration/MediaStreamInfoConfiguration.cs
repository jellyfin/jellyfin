using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// People configuration.
/// </summary>
public class MediaStreamInfoConfiguration : IEntityTypeConfiguration<MediaStreamInfo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MediaStreamInfo> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.StreamIndex });
        builder.HasIndex(e => e.StreamIndex);
        builder.HasIndex(e => e.StreamType);
        builder.HasIndex(e => new { e.StreamIndex, e.StreamType });
        builder.HasIndex(e => new { e.StreamIndex, e.StreamType, e.Language });
    }
}
