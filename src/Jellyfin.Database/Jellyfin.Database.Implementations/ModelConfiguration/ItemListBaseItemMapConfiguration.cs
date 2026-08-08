using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the ItemListBaseItemMap entity.
/// </summary>
public class ItemListBaseItemMapConfiguration : IEntityTypeConfiguration<ItemListBaseItemMap>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ItemListBaseItemMap> builder)
    {
        builder.HasKey(e => new { e.ItemListId, e.CustomDataKey });
        builder.HasIndex(e => e.ItemId);
        builder.HasIndex(e => new { e.ItemListId, e.SortIndex });
        builder.Property(e => e.CustomDataKey).IsRequired();
        builder
            .HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(e => e.ItemList)
            .WithMany()
            .HasForeignKey(e => e.ItemListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
