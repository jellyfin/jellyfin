using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// LinkedChildEntity configuration.
/// </summary>
public class LinkedChildConfiguration : IEntityTypeConfiguration<LinkedChildEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LinkedChildEntity> builder)
    {
        builder.ToTable("LinkedChildren");
        builder.HasKey(e => new { e.ParentId, e.ChildId });
        builder.HasIndex(e => new { e.ParentId, e.SortOrder });
        builder.HasIndex(e => new { e.ParentId, e.ChildType });
        builder.HasIndex(e => new { e.ChildId, e.ChildType });

        builder.HasOne(e => e.Parent)
            .WithMany(e => e.LinkedChildEntities)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Child)
            .WithMany(e => e.LinkedChildOfEntities)
            .HasForeignKey(e => e.ChildId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
