using System;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// People configuration.
/// </summary>
public class PeopleConfiguration : IEntityTypeConfiguration<People>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<People> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.Role, e.ListOrder });
        builder.HasIndex(e => new { e.ItemId, e.ListOrder });
        builder.HasIndex(e => e.Name);
    }
}
