using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration
{
    /// <summary>
    /// FluentAPI configuration for the ShareLink entity.
    /// </summary>
    public class ShareLinkConfiguration : IEntityTypeConfiguration<ShareLink>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<ShareLink> builder)
        {
            builder
                .Property(shareLink => shareLink.Id)
                .HasMaxLength(32);

            builder
                .HasIndex(shareLink => shareLink.UserId);

            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(shareLink => shareLink.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
