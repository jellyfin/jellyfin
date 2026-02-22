using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration
{
    /// <summary>
    /// FluentAPI configuration for the Permission entity.
    /// </summary>
    public class PreferenceConfiguration : IEntityTypeConfiguration<Preference>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Preference> builder)
        {
            builder
                .HasIndex(p => new { p.UserId, p.Kind })
                .HasFilter("[UserId] IS NOT NULL")
                .IsUnique();
        }
    }
}
