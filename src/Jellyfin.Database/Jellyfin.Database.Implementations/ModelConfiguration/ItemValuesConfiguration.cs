using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// itemvalues Configuration.
/// </summary>
public class ItemValuesConfiguration : IEntityTypeConfiguration<ItemValue>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ItemValue> builder)
    {
        builder.HasKey(e => e.ItemValueId);
        builder.Property(e => e.Value).HasMaxLength(KeyLengths.ItemValue);
        builder.Property(e => e.CleanValue).IsUnboundedIndexedText();
        builder.HasIndex(e => new { e.Type, e.CleanValue });
        builder.HasIndex(e => new { e.Type, e.Value }).IsUnique();
    }
}
