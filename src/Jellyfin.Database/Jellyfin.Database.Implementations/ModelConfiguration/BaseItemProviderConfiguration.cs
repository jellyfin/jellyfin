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
        builder.HasIndex(e => new { e.ProviderId, e.ProviderValue, e.ItemId });
    }
}
