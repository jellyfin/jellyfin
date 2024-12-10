using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLitePCL;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// Configuration for BaseItem.
/// </summary>
public class BaseItemConfiguration : IEntityTypeConfiguration<BaseItemEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemEntity> builder)
    {
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
        builder.HasMany(e => e.ParentAncestors);
        builder.HasMany(e => e.Children);
        builder.HasMany(e => e.LockedFields);
        builder.HasMany(e => e.TrailerTypes);
        builder.HasMany(e => e.Images);

        builder.HasIndex(e => e.Path);
        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.PresentationUniqueKey);
        builder.HasIndex(e => new { e.Id, e.Type, e.IsFolder, e.IsVirtualItem });

        // covering index
        builder.HasIndex(e => new { e.TopParentId, e.Id });
        // series
        builder.HasIndex(e => new { e.Type, e.SeriesPresentationUniqueKey, e.PresentationUniqueKey, e.SortName });
        // series counts
        // seriesdateplayed sort order
        builder.HasIndex(e => new { e.Type, e.SeriesPresentationUniqueKey, e.IsFolder, e.IsVirtualItem });
        // live tv programs
        builder.HasIndex(e => new { e.Type, e.TopParentId, e.StartDate });
        // covering index for getitemvalues
        builder.HasIndex(e => new { e.Type, e.TopParentId, e.Id });
        // used by movie suggestions
        builder.HasIndex(e => new { e.Type, e.TopParentId, e.PresentationUniqueKey });
        // latest items
        builder.HasIndex(e => new { e.Type, e.TopParentId, e.IsVirtualItem, e.PresentationUniqueKey, e.DateCreated });
        builder.HasIndex(e => new { e.IsFolder, e.TopParentId, e.IsVirtualItem, e.PresentationUniqueKey, e.DateCreated });
        // resume
        builder.HasIndex(e => new { e.MediaType, e.TopParentId, e.IsVirtualItem, e.PresentationUniqueKey });
    }
}
