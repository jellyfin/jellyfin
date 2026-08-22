using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// BaseItemTag configuration.
/// </summary>
public class BaseItemTagConfiguration : IEntityTypeConfiguration<BaseItemTag>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemTag> builder)
    {
        // On the value as written, so an item keeps two spellings of one tag.
        builder.HasKey(e => new { e.ItemId, e.Value });
        builder.HasOne(e => e.Item);
        builder.HasIndex(e => new { e.CleanValue, e.ItemId });
    }
}
