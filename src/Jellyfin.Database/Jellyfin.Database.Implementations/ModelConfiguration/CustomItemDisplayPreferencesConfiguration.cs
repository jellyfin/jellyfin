using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration
{
    /// <summary>
    /// FluentAPI configuration for the CustomItemDisplayPreferences entity.
    /// </summary>
    public class CustomItemDisplayPreferencesConfiguration : IEntityTypeConfiguration<CustomItemDisplayPreferences>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<CustomItemDisplayPreferences> builder)
        {
            builder
                .HasIndex(entity => new { entity.UserId, entity.ItemId, entity.Client, entity.Key })
                .IsUnique();
        }
    }
}
