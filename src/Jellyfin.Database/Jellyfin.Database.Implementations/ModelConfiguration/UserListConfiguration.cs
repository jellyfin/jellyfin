using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the UserList entity.
/// </summary>
public class UserListConfiguration : IEntityTypeConfiguration<UserList>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserList> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.SortIndex });
    }
}
