using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// BaseItemStudio configuration.
/// </summary>
public class BaseItemStudioConfiguration : IEntityTypeConfiguration<BaseItemStudio>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemStudio> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.StudioItemId });
        builder.HasOne(e => e.Item);
        builder.HasIndex(e => new { e.StudioItemId, e.ItemId });
    }
}
