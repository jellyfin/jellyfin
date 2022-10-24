using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the ActivityLog entity.
/// </summary>
public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasIndex(entity => entity.DateCreated);
    }
}
