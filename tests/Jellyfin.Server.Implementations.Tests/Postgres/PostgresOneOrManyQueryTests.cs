using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed class PostgresOneOrManyQueryTests : DatabaseTestFixture
{
    [ProviderTheory(TestDbProvider.Postgres)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(1000)]
    public async Task WhereOneOrMany_ListOfAnySize_ReturnsTheMatchingRows(int count)
    {
        var seeded = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        await using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(seeded.Select(id => new BaseItemEntity { Id = id, Type = "Movie" }));
            await context.SaveChangesAsync();
        }

        // Ids that do not exist pad the list, so large lists are checked as well.
        var requested = seeded.Take(count).Concat(Enumerable.Range(0, Math.Max(0, count - seeded.Count)).Select(_ => Guid.NewGuid())).ToList();
        Assert.Equal(count, requested.Count);

        await using (var context = CreateDbContext())
        {
            var found = await context.BaseItems.WhereOneOrMany(requested, e => e.Id).Select(e => e.Id).ToListAsync();

            Assert.Equal(seeded.Take(count).Order(), found.Order());
        }
    }

    [ProviderTheory(TestDbProvider.Postgres)]
    [InlineData(0, "FALSE")]
    [InlineData(1, "= @")]
    [InlineData(2, "= ANY (@")]
    [InlineData(1000, "= ANY (@")]
    public async Task WhereOneOrMany_ListOfAnySize_BindsTheValuesAsParameters(int count, string expectedFragment)
    {
        await using var context = CreateDbContext();
        var requested = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

        // The values of the parameters are listed in comment lines in front of the statement.
        var sql = string.Join(
            '\n',
            context.BaseItems.WhereOneOrMany(requested, e => e.Id).Select(e => e.Id).ToQueryString()
                .Split('\n')
                .Where(line => !line.StartsWith("--", StringComparison.Ordinal)));

        Assert.Contains(expectedFragment, sql, StringComparison.Ordinal);
        Assert.All(requested, id => Assert.DoesNotContain(id.ToString(), sql, StringComparison.OrdinalIgnoreCase));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task WhereOneOrMany_NullableGuidProperty_MatchesOnlyTheListedValues()
    {
        var series = Guid.NewGuid();
        var otherSeries = Guid.NewGuid();
        var inSeries = Guid.NewGuid();
        var inOtherSeries = Guid.NewGuid();
        var withoutSeries = Guid.NewGuid();
        await using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(
                new BaseItemEntity { Id = inSeries, Type = "Movie", SeriesId = series },
                new BaseItemEntity { Id = inOtherSeries, Type = "Movie", SeriesId = otherSeries },
                new BaseItemEntity { Id = withoutSeries, Type = "Movie" });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            IReadOnlyList<Guid> seriesIds = [series, Guid.NewGuid()];

            var found = await context.BaseItems.WhereOneOrMany(seriesIds, e => e.SeriesId!.Value).Select(e => e.Id).ToListAsync();

            Assert.Equal([inSeries], found);
        }
    }
}
