using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the WaveformInfo entity.
/// </summary>
public class WaveformInfoConfiguration : IEntityTypeConfiguration<WaveformInfo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<WaveformInfo> builder)
    {
        builder.HasKey(e => e.ItemId);
        builder.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId);
    }
}
