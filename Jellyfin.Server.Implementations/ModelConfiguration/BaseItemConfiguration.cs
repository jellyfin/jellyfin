using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// Configuration for BaseItem.
/// </summary>
public class BaseItemConfiguration : IEntityTypeConfiguration<BaseItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItem> builder)
    {
        builder.HasNoKey();
        builder.HasIndex(e => e.Path);
        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.PresentationUniqueKey);
        builder.HasIndex(e => new { e.Id, e.Type, e.IsFolder, e.IsVirtualItem });
        builder.HasIndex(e => new { e.UserDataKey, e.Type });

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
