using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Server.Implementations.ModelConfiguration
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
                .Property(s => s.TypeIndex)
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
                .HasKey(s => new { s.ItemId, s.StreamIndex, s.Type, s.TypeIndex });
            builder
                .HasIndex(s => s.ItemId);
        }
    }
}
