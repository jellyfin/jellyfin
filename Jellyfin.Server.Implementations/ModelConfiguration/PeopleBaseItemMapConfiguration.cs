using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// People configuration.
/// </summary>
public class PeopleBaseItemMapConfiguration : IEntityTypeConfiguration<PeopleBaseItemMap>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<PeopleBaseItemMap> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.PeopleId });
        builder.HasIndex(e => new { e.ItemId, e.SortOrder });
        builder.HasIndex(e => new { e.ItemId, e.ListOrder });
        builder.HasOne(e => e.Item);
        builder.HasOne(e => e.People);
    }
}
