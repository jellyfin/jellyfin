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
        builder.HasOne(e => e.Item);
        builder.HasIndex(e => new { e.ProviderId, e.ItemId, e.ProviderValue });
        // Resolving a credit to the person it belongs to looks an item up by the value of a provider's
        // id. The covering index above leads with the item id, so it cannot seek on the value.
        builder.HasIndex(e => new { e.ProviderId, e.ProviderValue });
    }
}
