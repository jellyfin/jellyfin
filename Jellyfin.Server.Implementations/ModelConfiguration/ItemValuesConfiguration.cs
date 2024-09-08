using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// itemvalues Configuration.
/// </summary>
public class ItemValuesConfiguration : IEntityTypeConfiguration<ItemValue>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ItemValue> builder)
    {
        builder.HasNoKey();
        builder.HasIndex(e => new { e.ItemId, e.Type, e.CleanValue });
        builder.HasIndex(e => new { e.ItemId, e.Type, e.Value });
    }
}
