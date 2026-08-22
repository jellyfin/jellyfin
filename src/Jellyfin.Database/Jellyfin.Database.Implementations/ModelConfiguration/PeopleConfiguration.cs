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
        // A person is looked up and deduplicated by its clean name, paired with the type for the
        // lookups that keep an Actor and a Director credit for the same human apart.
        builder.HasIndex(e => new { e.CleanName, e.PersonType });
        // Every query that asks "what is this person credited on" starts from the person item id.
        builder.HasIndex(e => e.ItemId);
        builder.HasMany(e => e.BaseItems);
    }
}
