using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A row the database cannot turn into an entity used to abort the whole migration. Callers that opt in
/// now skip it and carry on, and the walk must not stop early because of the gap it leaves.
/// </summary>
public sealed class PartitionSkipUnreadableTests : SqliteDbTestFixture
{
    private const string UnreadableDate = "2023-01-17 03:02:94.3383473";
    private const int PartitionSize = 2;

    private const string TestType = "PartitionSkipUnreadableTests";

    private readonly Guid[] _ids =
    [
        .. Enumerable.Range(1, 5).Select(i => Guid.Parse($"10000000-0000-0000-0000-00000000000{i}"))
    ];

    public PartitionSkipUnreadableTests()
    {
        using var context = CreateDbContext();
        foreach (var id in _ids)
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                Type = TestType,
                Name = id.ToString("N"),
                PresentationUniqueKey = id.ToString("N"),
                DateCreated = DateTime.UtcNow
            });
        }

        context.SaveChanges();

        // The second row, so its partition is still full and the walk has to continue past it.
        context.Database.ExecuteSqlRaw(
            "UPDATE BaseItems SET DateCreated = {0} WHERE Id = {1}", UnreadableDate, _ids[1]);
    }

    [Fact]
    public async Task PartitionEagerAsync_OptedIn_ReportsTheRowKeyAndKeepsGoing()
    {
        var failures = new List<(object? Key, int Row)>();
        var seen = new List<Guid>();

        await using var context = CreateDbContext();
        await foreach (var item in Query(context)
                           .SkippingUnreadableItems(e => e.Id, (_, key, row) => failures.Add((key, row)))
                           .PartitionEagerAsync(PartitionSize, TestContext.Current.CancellationToken)
                           .WithCancellation(TestContext.Current.CancellationToken)
                           .ConfigureAwait(false))
        {
            seen.Add(item.Id);
        }

        // The row is named by its key, not just its offset, so it can be found without counting.
        Assert.Equal([(_ids[1], 1)], failures);
        // Everything but the unreadable row, including the rows in later partitions.
        Assert.Equal(_ids.Where(id => !id.Equals(_ids[1])), seen);
    }

    [Fact]
    public async Task PartitionEagerAsync_NotOptedIn_StillFails()
    {
        await using var context = CreateDbContext();

        await Assert.ThrowsAsync<FormatException>(async () =>
        {
            await foreach (var item in Query(context)
                               .PartitionEagerAsync(PartitionSize, cancellationToken: TestContext.Current.CancellationToken)
                               .WithCancellation(TestContext.Current.CancellationToken)
                               .ConfigureAwait(false))
            {
                _ = item;
            }
        }).ConfigureAwait(true);
    }

    private static IOrderedQueryable<BaseItemEntity> Query(JellyfinDbContext context)
        => context.BaseItems.Where(e => e.Type == TestType).OrderBy(e => e.Id);
}
