using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the AttachmentStreamInfo entity.
/// </summary>
public class AttachmentStreamInfoConfiguration : IEntityTypeConfiguration<AttachmentStreamInfo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AttachmentStreamInfo> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.Index });
    }
}
