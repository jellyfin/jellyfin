using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// Chapter configuration.
/// </summary>
public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.ChapterIndex });
        builder.HasOne(e => e.Item);
    }
}
