using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// itemvalues Configuration.
/// </summary>
public class ItemValuesMapConfiguration : IEntityTypeConfiguration<ItemValueMap>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ItemValueMap> builder)
    {
        builder.HasKey(e => new { e.ItemValueId, e.ItemId });
        builder.HasOne(e => e.Item);
        builder.HasOne(e => e.ItemValue);
    }
}
