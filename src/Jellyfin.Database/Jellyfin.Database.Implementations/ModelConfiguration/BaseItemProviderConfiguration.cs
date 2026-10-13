using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// BaseItemProvider configuration.
/// </summary>
public class BaseItemProviderConfiguration : IEntityTypeConfiguration<BaseItemProvider>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemProvider> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.ProviderId });
        builder.Property(e => e.ProviderId).HasMaxLength(KeyLengths.ProviderId);
        builder.Property(e => e.ProviderValue).IsUnboundedIndexedText();
        builder.HasOne(e => e.Item);
        builder.HasIndex(e => new { e.ProviderId, e.ItemId, e.ProviderValue });
    }
}
