using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// Company to BaseItem mapping configuration.
/// </summary>
public class CompanyBaseItemMapConfiguration : IEntityTypeConfiguration<CompanyBaseItemMap>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CompanyBaseItemMap> builder)
    {
        // A company can be credited twice on one item, once per kind.
        builder.HasKey(e => new { e.ItemId, e.CompanyId, e.Type });
        builder.HasIndex(e => new { e.CompanyId, e.ItemId });
        builder.HasIndex(e => e.Type);
        builder.HasOne(e => e.Item);
        builder.HasOne(e => e.Company);
    }
}
