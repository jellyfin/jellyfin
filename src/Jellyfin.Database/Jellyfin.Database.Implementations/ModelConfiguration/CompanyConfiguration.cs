using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// Company configuration.
/// </summary>
public class CompanyConfiguration : IEntityTypeConfiguration<CompanyEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CompanyEntity> builder)
    {
        builder.HasKey(e => e.Id);

        // One company per name: what it did belongs to the credit, not to the company.
        builder.HasIndex(e => e.CleanName).IsUnique();
        builder.HasMany(e => e.BaseItems);
    }
}
