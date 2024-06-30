using Jellyfin.Data.Entities.MediaSegment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration.MediaSegmentConfiguration
{
    /// <summary>
    /// FluentAPI configuration for the MediaSegment entity.
    /// </summary>
    public class MediaSegmentConfiguration : IEntityTypeConfiguration<MediaSegment>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<MediaSegment> builder)
        {
            builder
                .Property(s => s.StartTicks)
                .IsRequired();
            builder
                .Property(s => s.EndTicks)
                .IsRequired();
            builder
                .Property(s => s.Type)
                .IsRequired();
            builder
                .Property(s => s.ItemId)
                .IsRequired();
            builder
                .Property(s => s.StreamIndex)
                .IsRequired();
            builder
                .Property(s => s.Action)
                .IsRequired();
            builder
                .HasKey(s => s.Id);
            builder
                .HasIndex(s => s.ItemId);
        }
    }
}
