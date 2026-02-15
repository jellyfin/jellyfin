using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the UserData entity.
/// </summary>
public class UserDataConfiguration : IEntityTypeConfiguration<UserData>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserData> builder)
    {
        builder.HasKey(d => new { d.ItemId, d.UserId, d.CustomDataKey });
        builder.HasIndex(d => new { d.ItemId, d.UserId, d.Played });
        builder.HasIndex(d => new { d.ItemId, d.UserId, d.PlaybackPositionTicks });
        builder.HasIndex(d => new { d.ItemId, d.UserId, d.IsFavorite });
        builder.HasIndex(d => new { d.ItemId, d.UserId, d.LastPlayedDate });
        builder.HasOne(e => e.Item).WithMany(e => e.UserData);
    }
}
