using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration
{
    /// <summary>
    /// FluentAPI configuration for the Permission entity.
    /// </summary>
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            // Used to get a user's permissions or a specific permission for a user.
            // Also prevents multiple values being created for a user.
            // Filtered over non-null user ids for when other entities (groups, API keys) get permissions
            builder
                .HasIndex(p => new { p.UserId, p.Kind })
                .HasFilter("[UserId] IS NOT NULL")
                .IsUnique();
        }
    }
}
