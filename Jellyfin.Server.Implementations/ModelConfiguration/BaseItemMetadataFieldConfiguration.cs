using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// Provides configuration for the BaseItemMetadataField entity.
/// </summary>
public class BaseItemMetadataFieldConfiguration : IEntityTypeConfiguration<BaseItemMetadataField>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemMetadataField> builder)
    {
        builder.HasKey(e => new { e.Id, e.ItemId });
        builder.HasOne(e => e.Item);
    }
}
