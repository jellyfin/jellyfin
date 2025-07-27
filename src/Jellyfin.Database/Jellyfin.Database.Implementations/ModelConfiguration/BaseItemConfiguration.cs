using System;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// Configuration for BaseItem.
/// </summary>
public class BaseItemConfiguration : IEntityTypeConfiguration<BaseItemEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemEntity> builder)
    {
        builder.Property(b => b.ItemType).HasDefaultValue(-1);
        builder.HasKey(e => e.Id);
        // TODO: See rant in entity file.
        // builder.HasOne(e => e.Parent).WithMany(e => e.DirectChildren).HasForeignKey(e => e.ParentId);
        // builder.HasOne(e => e.TopParent).WithMany(e => e.AllChildren).HasForeignKey(e => e.TopParentId);
        // builder.HasOne(e => e.Season).WithMany(e => e.SeasonEpisodes).HasForeignKey(e => e.SeasonId);
        // builder.HasOne(e => e.Series).WithMany(e => e.SeriesEpisodes).HasForeignKey(e => e.SeriesId);
        builder.HasMany(e => e.Peoples);
        builder.HasMany(e => e.UserData);
        builder.HasMany(e => e.ItemValues);
        builder.HasMany(e => e.MediaStreams);
        builder.HasMany(e => e.Chapters);
        builder.HasMany(e => e.Provider);
        builder.HasMany(e => e.Parents);
        builder.HasMany(e => e.Children);
        builder.HasMany(e => e.LockedFields);
        builder.HasMany(e => e.TrailerTypes);
        builder.HasMany(e => e.Images);

        builder
        .HasOne<BaseItemKindEntity>()
        .WithMany()
        .HasForeignKey(b => b.ItemType)
        .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(e => e.Path);
        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.PresentationUniqueKey);

        // covering index
        builder.HasIndex(e => new { e.TopParentId, e.Id });
        // series
        builder.HasIndex(e => new { e.ItemType, e.SeriesPresentationUniqueKey, e.PresentationUniqueKey, e.SortName });

        builder.HasIndex(e => new { e.ItemType, e.SeriesPresentationUniqueKey, e.IsFolder, e.IsVirtualItem });

        builder.HasIndex(e => new { e.ItemType, e.TopParentId });
        // latest items
        builder.HasIndex(e => new { e.IsFolder, e.TopParentId, e.IsVirtualItem, e.PresentationUniqueKey, e.DateCreated });
        // resume
        builder.HasIndex(e => new { e.MediaType, e.TopParentId, e.IsVirtualItem, e.PresentationUniqueKey });

        builder.HasData(new BaseItemEntity()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ItemType = -1,
            Name = "This is a placeholder item for UserData that has been detected from its original item",
        });
    }
}
