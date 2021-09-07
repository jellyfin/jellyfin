using Jellyfin.Data.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration
{
    /// <summary>
    /// FluentAPI configuration for the DeviceOptions entity.
    /// </summary>
    public class DeviceOptionsConfiguration : IEntityTypeConfiguration<DeviceOptions>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<DeviceOptions> builder)
        {
            builder
                .HasIndex(entity => entity.DeviceId)
                .IsUnique();
        }
    }
}
