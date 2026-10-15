using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: a query, a delete and an update that filter on a list of thousands of ids find exactly the rows a short
/// list would, through <see cref="JellyfinQueryHelperExtensions.WhereOneOrMany"/>, which sends the list as one
/// parameter; and a plain <c>Contains</c> over the same list does as well, which an engine that gets a parameter for
/// every value cannot do beyond its limit of parameters.
/// </summary>
public abstract class IdListConformance : ConformanceTestBase
{
    private const int Stored = 25;

    // The most parameters SQLite takes in one statement.
    private const int SqliteParameterLimit = 32_766;

    [ConformanceTheory]
    [InlineData(3_000)]
    [InlineData(100_000)]
    public void WhereOneOrMany_GuidListOfManyIds_FindsAndDeletesExactlyTheStoredOnes(int count)
    {
        var (stored, ids) = StoreItems(count);

        using (var context = CreateDbContext())
        {
            Assert.Equal(stored, context.BaseItems.WhereOneOrMany(ids, e => e.Id).Select(e => e.Id).AsEnumerable().Order().ToList());
            Assert.Equal(Stored, context.BaseItems.WhereOneOrMany(ids, e => e.Id).Count());
            Assert.Equal(Stored, context.BaseItems.WhereOneOrMany(ids, e => e.Id).ExecuteDelete());
        }

        using var verification = CreateDbContext();
        Assert.Equal(0, verification.BaseItems.WhereOneOrMany(stored, e => e.Id).Count());
    }

    [ConformanceTheory]
    [InlineData(3_000)]
    [InlineData(100_000)]
    public void WhereOneOrMany_IntegerListOfManyIds_FindsAndUpdatesExactlyTheStoredOnes(int count)
    {
        var (stored, ids) = StoreLogs(count);

        using (var context = CreateDbContext())
        {
            Assert.Equal(stored, context.ActivityLogs.WhereOneOrMany(ids, e => e.Id).Select(e => e.Id).AsEnumerable().Order().ToList());
            Assert.Equal(Stored, context.ActivityLogs.WhereOneOrMany(ids, e => e.Id).ExecuteUpdate(e => e.SetProperty(log => log.Name, "updated")));
        }

        using var verification = CreateDbContext();
        Assert.Equal(Stored, verification.ActivityLogs.Count(e => e.Name == "updated"));
    }

    [ConformanceTheory]
    [InlineData(3_000)]
    [InlineData(100_000)]
    public void Contains_GuidListOfManyIds_FindsExactlyTheStoredOnes(int count)
    {
        var (stored, ids) = StoreItems(count);

        OverParameterLimit(count, () =>
        {
            using var context = CreateDbContext();
            Assert.Equal(stored, context.BaseItems.Where(e => ids.Contains(e.Id)).Select(e => e.Id).AsEnumerable().Order().ToList());
        });
    }

    [ConformanceTheory]
    [InlineData(3_000)]
    [InlineData(100_000)]
    public void Contains_IntegerListOfManyIds_FindsExactlyTheStoredOnes(int count)
    {
        var (stored, ids) = StoreLogs(count);

        OverParameterLimit(count, () =>
        {
            using var context = CreateDbContext();
            Assert.Equal(stored, context.ActivityLogs.Where(e => ids.Contains(e.Id)).Select(e => e.Id).AsEnumerable().Order().ToList());
        });
    }

    private static void OverParameterLimit(int count, Action body)
    {
        if (count > SqliteParameterLimit)
        {
            KnownGap.On(KnownGaps.ContainsOverManyValues, body);
        }
        else
        {
            body();
        }
    }

    private (List<Guid> Stored, List<Guid> Ids) StoreItems(int count)
    {
        var items = Table<BaseItemEntity>();
        var stored = Enumerable.Range(0, Stored).Select(_ => (Guid)Rows.Save(items).Key.Single()!).Order().ToList();
        var ids = stored.Concat(Enumerable.Range(0, count - Stored).Select(_ => Guid.NewGuid())).Reverse().ToList();
        return (stored, ids);
    }

    private (List<int> Stored, List<int> Ids) StoreLogs(int count)
    {
        var logs = Table<ActivityLog>();
        var stored = Enumerable.Range(0, Stored).Select(_ => (int)Rows.Save(logs).Key.Single()!).Order().ToList();
        var ids = stored.Concat(Enumerable.Range(1_000_000, count - Stored)).Reverse().ToList();
        return (stored, ids);
    }
}
