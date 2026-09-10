using Jellyfin.Database.Implementations.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the OIDC external identity entity.
/// </summary>
public class OidcExternalIdentityConfiguration : IEntityTypeConfiguration<OidcExternalIdentity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OidcExternalIdentity> builder)
    {
        builder
            .HasIndex(entity => new { entity.ProviderId, entity.Issuer, entity.Subject })
            .IsUnique();

        builder
            .HasIndex(entity => new { entity.UserId, entity.ProviderId })
            .IsUnique();

        builder
            .HasOne(entity => entity.User)
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
