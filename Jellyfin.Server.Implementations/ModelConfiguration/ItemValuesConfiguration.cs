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
        builder.HasKey(e => e.ItemValueId);
        builder.HasIndex(e => new { e.Type, e.CleanValue }).IsUnique();
    }
}
