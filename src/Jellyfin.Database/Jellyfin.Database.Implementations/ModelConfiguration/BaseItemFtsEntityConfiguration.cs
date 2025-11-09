using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// Configuration for BaseItemFtsEntity - maps to the FTS5 virtual table.
/// </summary>
public class BaseItemFtsEntityConfiguration : IEntityTypeConfiguration<BaseItemFtsEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemFtsEntity> builder)
    {
        builder.ToTable("BaseItems_fts");

        builder.HasKey(e => e.RowId);
        builder.Property(e => e.RowId).HasColumnName("rowid");

        builder.Property(e => e.Match).HasColumnName("BaseItems_fts");
        builder.Property(e => e.Rank).HasColumnName("rank");
        builder.Property(e => e.Id).HasColumnName("Id");

        builder.HasOne(e => e.BaseItem)
            .WithMany()
            .HasForeignKey(e => e.Id)
            .HasPrincipalKey(b => b.Id);
    }
}
