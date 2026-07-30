using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the UserListItem entity.
/// </summary>
public class UserListItemConfiguration : IEntityTypeConfiguration<UserListItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserListItem> builder)
    {
        builder.HasKey(e => new { e.UserListId, e.CustomDataKey });
        builder.HasIndex(e => e.ItemId);
        builder.HasIndex(e => new { e.UserListId, e.SortIndex });
        builder.Property(e => e.CustomDataKey).IsRequired();
        builder
            .HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(e => e.UserList)
            .WithMany()
            .HasForeignKey(e => e.UserListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
