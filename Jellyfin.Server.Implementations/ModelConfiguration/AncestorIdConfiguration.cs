using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// AncestorId configuration.
/// </summary>
public class AncestorIdConfiguration : IEntityTypeConfiguration<AncestorId>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AncestorId> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.Id });
        builder.HasIndex(e => e.Id);
        builder.HasIndex(e => new { e.ItemId, e.AncestorIdText });
    }
}
