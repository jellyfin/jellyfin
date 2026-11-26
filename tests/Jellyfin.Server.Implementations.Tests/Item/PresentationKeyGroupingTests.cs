using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Verifies the presentation key grouping against the SQLite provider: which ids it keeps, and that the
/// GROUP BY can't be served from the presentation key index.
/// </summary>
public sealed class PresentationKeyGroupingTests : SqliteDbTestFixture
{
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";

    [Fact]
    public void GetGroupedIdsByPresentationKey_Versions_KeepsThePrimary()
    {
        // The primary gets the higher id, so a plain MIN(Id) would pick the alternate.
        var (alternate, primary) = TwoIdsInOrder();
        var other = Guid.NewGuid();
        var key = NewKey();

        using (var ctx = CreateDbContext())
        {
            AddItem(ctx, primary, key);
            AddItem(ctx, alternate, key, primaryVersionId: primary);
            AddItem(ctx, other, NewKey());
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var ids = BaseItemRepository.GetGroupedIdsByPresentationKey(Items(ctx, primary, alternate, other)).ToList();

            Assert.Equal(new Guid?[] { primary, other }.Order(), ids.Order());
        }
    }

    [Fact]
    public void GetGroupedIdsByPresentationKey_NullKeys_FormOneGroup()
    {
        var (first, second) = TwoIdsInOrder();

        using (var ctx = CreateDbContext())
        {
            AddItem(ctx, first, null);
            AddItem(ctx, second, null);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var ids = BaseItemRepository.GetGroupedIdsByPresentationKey(Items(ctx, first, second)).ToList();

            Assert.Equal(new Guid?[] { first }, ids);
        }
    }

    [Fact]
    public void GetGroupedIdsByPresentationAndSeriesKey_DifferentSeries_KeepsBoth()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var key = NewKey();

        using (var ctx = CreateDbContext())
        {
            AddItem(ctx, first, key, seriesKey: NewKey());
            AddItem(ctx, second, key, seriesKey: NewKey());
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var ids = BaseItemRepository.GetGroupedIdsByPresentationAndSeriesKey(Items(ctx, first, second)).ToList();

            Assert.Equal(new Guid?[] { first, second }.Order(), ids.Order());
        }
    }

    [Fact]
    public void GetGroupedIdsByPresentationKey_QueryPlan_DoesNotWalkThePresentationKeyIndex()
    {
        using var ctx = CreateDbContext();

        var sql = BaseItemRepository.GetGroupedIdsByPresentationKey(ctx.BaseItems.AsNoTracking()).ToQueryString();

        Assert.DoesNotContain("IX_BaseItems_PresentationUniqueKey", QueryPlan(ctx, sql), StringComparison.Ordinal);
    }

    [Fact]
    public void GetGroupedIdsByPresentationAndSeriesKey_QueryPlan_DoesNotWalkThePresentationKeyIndex()
    {
        using var ctx = CreateDbContext();

        var sql = BaseItemRepository.GetGroupedIdsByPresentationAndSeriesKey(ctx.BaseItems.AsNoTracking()).ToQueryString();

        Assert.DoesNotContain("IX_BaseItems_PresentationUniqueKey", QueryPlan(ctx, sql), StringComparison.Ordinal);
    }

    private static IQueryable<BaseItemEntity> Items(JellyfinDbContext context, params Guid[] ids)
        => context.BaseItems.AsNoTracking().Where(e => ids.Contains(e.Id));

    private static string NewKey() => Guid.NewGuid().ToString("N");

    private static (Guid Lower, Guid Higher) TwoIdsInOrder()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        // SQLite stores the ids as upper case text, so MIN() compares them as strings.
        var aText = a.ToString().ToUpperInvariant();
        var bText = b.ToString().ToUpperInvariant();

        return string.CompareOrdinal(aText, bText) < 0 ? (a, b) : (b, a);
    }

    private static string QueryPlan(JellyfinDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN " + sql;

        var lines = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lines.Add(reader.GetString(3));
        }

        return string.Join('\n', lines);
    }

    private static void AddItem(
        JellyfinDbContext context,
        Guid id,
        string? presentationKey,
        Guid? primaryVersionId = null,
        string? seriesKey = null)
        => context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = MovieType,
            Name = MovieType + " " + id,
            PresentationUniqueKey = presentationKey,
            SeriesPresentationUniqueKey = seriesKey,
            PrimaryVersionId = primaryVersionId
        });
}
