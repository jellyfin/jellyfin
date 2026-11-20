using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// BaseItemGenre configuration.
/// </summary>
public class BaseItemGenreConfiguration : IEntityTypeConfiguration<BaseItemGenre>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemGenre> builder)
    {
        builder.HasKey(e => new { e.ItemId, e.GenreItemId });
        builder.HasOne(e => e.Item);
        builder.HasIndex(e => new { e.GenreItemId, e.ItemId });
    }
}
