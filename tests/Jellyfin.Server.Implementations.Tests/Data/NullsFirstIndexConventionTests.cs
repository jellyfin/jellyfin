using System.Linq;
using Jellyfin.Database.Providers.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public class NullsFirstIndexConventionTests
{
    [Fact]
    public void AscendingColumns_SortNullsFirst()
    {
        var index = BuildIndex(b => b.HasIndex(r => new { r.Name, r.Rank }));

        Assert.Equal([NullSortOrder.NullsFirst, NullSortOrder.NullsFirst], index.GetNullSortOrder());
    }

    [Fact]
    public void DescendingColumns_SortNullsLast()
    {
        var index = BuildIndex(b => b.HasIndex(r => new { r.Name, r.Rank }).IsDescending(true, true));

        Assert.Equal([NullSortOrder.NullsLast, NullSortOrder.NullsLast], index.GetNullSortOrder());
    }

    [Fact]
    public void MixedDirections_GetAnOrderPerColumn()
    {
        var index = BuildIndex(b => b.HasIndex(r => new { r.Name, r.Rank, r.Code }).IsDescending(false, true, false));

        Assert.Equal([NullSortOrder.NullsFirst, NullSortOrder.NullsLast, NullSortOrder.NullsFirst], index.GetNullSortOrder());
    }

    [Fact]
    public void NonNullableColumns_KeepTheDefaultOrder()
    {
        var ascending = BuildIndex(b => b.HasIndex(r => new { r.Id, r.Version }));
        var descending = BuildIndex(b => b.HasIndex(r => new { r.Id, r.Version }).IsDescending(true, true));

        Assert.All(ascending.GetNullSortOrder() ?? [], order => Assert.Equal(NullSortOrder.Unspecified, order));
        Assert.All(descending.GetNullSortOrder() ?? [], order => Assert.Equal(NullSortOrder.Unspecified, order));
    }

    [Fact]
    public void NullableAndNonNullableColumns_GetAnOrderOnlyForTheNullableOnes()
    {
        var index = BuildIndex(b => b.HasIndex(r => new { r.Version, r.Name, r.Id, r.Rank }).IsDescending(false, false, true, true));

        Assert.Equal(
            [NullSortOrder.Unspecified, NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.NullsLast],
            index.GetNullSortOrder());
    }

    [Fact]
    public void IndexWithAnotherAccessMethod_GetsNoNullOrder()
    {
        var hash = BuildIndex(b => b.HasIndex(r => r.Name).HasMethod("hash"));
        var btree = BuildIndex(b => b.HasIndex(r => r.Name).HasMethod("btree"));

        Assert.Null(hash.GetNullSortOrder());
        Assert.Equal([NullSortOrder.NullsFirst], btree.GetNullSortOrder());
    }

    private static IIndex BuildIndex(System.Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Row>> configure)
    {
        using var context = new DbContext(new DbContextOptionsBuilder().UseNpgsql("Host=localhost").Options);
        var conventions = context.GetService<IConventionSetBuilder>().CreateConventionSet();
        conventions.ModelFinalizingConventions.Add(new NullsFirstIndexConvention());
        var builder = new ModelBuilder(conventions);
        configure(builder.Entity<Row>());
        return builder.FinalizeModel().GetEntityTypes().Single().GetIndexes().Single();
    }

    private sealed class Row
    {
        public int Id { get; set; }

        public int Version { get; set; }

        public string? Name { get; set; }

        public int? Rank { get; set; }

        public string? Code { get; set; }
    }
}
