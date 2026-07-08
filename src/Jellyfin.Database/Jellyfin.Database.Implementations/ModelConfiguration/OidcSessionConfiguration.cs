using Jellyfin.Database.Implementations.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the OIDC session entity.
/// </summary>
public class OidcSessionConfiguration : IEntityTypeConfiguration<OidcSession>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OidcSession> builder)
    {
        builder
            .HasIndex(entity => entity.AccessToken)
            .IsUnique();

        builder
            .HasIndex(entity => new { entity.ProviderId, entity.Issuer, entity.Subject });

        builder
            .HasIndex(entity => entity.Sid);
    }
}
