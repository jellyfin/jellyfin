using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration
{
    /// <summary>
    /// Configuration for the UserHomeSection entity.
    /// </summary>
    public class UserHomeSectionConfiguration : IEntityTypeConfiguration<UserHomeSection>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<UserHomeSection> builder)
        {
            builder.ToTable("UserHomeSections");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.UserId)
                .IsRequired();

            builder.Property(e => e.SectionId)
                .IsRequired();

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(e => e.SectionType)
                .IsRequired();

            builder.Property(e => e.Priority)
                .IsRequired();

            builder.Property(e => e.MaxItems)
                .IsRequired();

            builder.Property(e => e.SortOrder)
                .IsRequired();

            builder.Property(e => e.SortBy)
                .IsRequired();

            // Create a unique index on UserId + SectionId
            builder.HasIndex(e => new { e.UserId, e.SectionId })
                .IsUnique();
        }
    }
}
