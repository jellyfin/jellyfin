using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// People configuration.
/// </summary>
public class PeopleConfiguration : IEntityTypeConfiguration<People>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<People> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Name);
        // Paired with the type, for the lookups keeping an Actor and a Director credit apart.
        builder.HasIndex(e => new { e.CleanName, e.PersonType });
        builder.HasIndex(e => e.ItemId);
        builder.HasMany(e => e.BaseItems);
    }
}
