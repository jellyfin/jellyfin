using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration
{
    /// <summary>
    /// Fluent API configuration for the SmartCollections entity.
    /// </summary>
    public class SmartCollectionsConfiguration : IEntityTypeConfiguration<SmartCollections>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<SmartCollections> builder)
        {
            builder.ToTable("SmartCollections");
            builder.HasAnnotation("Sqlite:UseSqlReturningClause", false);
            builder.HasIndex(entity => entity.UserId);
            builder.HasIndex(entity => new { entity.UserId, entity.Name }).IsUnique();
        }
    }
}
