using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemCountServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IApplicationPaths _applicationPaths;
    private readonly ItemCountService _service;

    public ItemCountServiceTests()
    {
        _applicationPaths = new Mock<IApplicationPaths>().Object;

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var queryHelpers = new Mock<IItemQueryHelpers>();
        queryHelpers
            .Setup(h => h.ApplyAccessFiltering(
                It.IsAny<JellyfinDbContext>(),
                It.IsAny<IQueryable<BaseItemEntity>>(),
                It.IsAny<InternalItemsQuery>()))
            .Returns((JellyfinDbContext _, IQueryable<BaseItemEntity> query, InternalItemsQuery _) => query);

        _service = new ItemCountService(
            factory.Object,
            new Mock<IItemTypeLookup>().Object,
            queryHelpers.Object);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void GetChildCountBatch_LargeParentIdSet_DoesNotExceedSqliteVariableLimit()
    {
        var hierarchicalParentId = Guid.NewGuid();
        var linkedParentId = Guid.NewGuid();

        var hierarchicalChildId = Guid.NewGuid();
        var linkedChildId1 = Guid.NewGuid();
        var linkedChildId2 = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(
                CreateItem(hierarchicalParentId),
                CreateItem(linkedParentId),
                CreateItem(hierarchicalChildId, hierarchicalParentId),
                CreateItem(linkedChildId1),
                CreateItem(linkedChildId2));

            context.LinkedChildren.AddRange(
                new LinkedChildEntity
                {
                    ParentId = linkedParentId,
                    ChildId = linkedChildId1,
                    ChildType = LinkedChildType.Manual,
                    SortOrder = 0
                },
                new LinkedChildEntity
                {
                    ParentId = linkedParentId,
                    ChildId = linkedChildId2,
                    ChildType = LinkedChildType.Manual,
                    SortOrder = 1
                });

            context.SaveChanges();
        }

        var parentIds = Enumerable.Range(0, 40_000)
            .Select(_ => Guid.NewGuid())
            .ToList();

        parentIds.Add(hierarchicalParentId);
        parentIds.Add(linkedParentId);

        var result = _service.GetChildCountBatch(parentIds, null);

        Assert.Equal(1, result[hierarchicalParentId]);
        Assert.Equal(2, result[linkedParentId]);
        Assert.Equal(parentIds.Count, result.Count);
    }

    [Fact]
    public void GetCounts_MergedFolders_CountLeavesOfEveryFolderInTheGroup()
    {
        // Two folder-items of one merged series: same presentation key, a leaf each, one of them played.
        var (user, seriesA, seriesB) = SeedMergedSeries(out var playedLeafId);

        var filter = new InternalItemsQuery(user);

        // Either folder-item stands for the whole merged series, so both must report the group.
        foreach (var seriesId in new[] { seriesA, seriesB })
        {
            Assert.Equal(2, _service.GetTotalCount(filter, seriesId));
            Assert.Equal(1, _service.GetPlayedCount(filter, seriesId));
            Assert.Equal((1, 2), _service.GetPlayedAndTotalCount(filter, seriesId));
        }

        var batch = _service.GetPlayedAndTotalCountBatch([seriesA], user);
        Assert.Equal((1, 2), batch[seriesA]);

        Assert.NotEqual(Guid.Empty, playedLeafId);
    }

    [Fact]
    public void GetCounts_UnmergedFolder_CountsOnlyItsOwnLeaves()
    {
        var (user, _, _) = SeedMergedSeries(out _);

        // A folder with a key of its own must not pick up anything from the merged pair.
        var loneSeriesId = Guid.NewGuid();
        var loneLeafId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            var lone = CreateItem(loneSeriesId);
            lone.PresentationUniqueKey = "lone-series";
            context.BaseItems.Add(lone);
            context.BaseItems.Add(CreateLeaf(loneLeafId));
            context.SaveChanges();
            AddAncestor(context, loneLeafId, loneSeriesId);
            context.SaveChanges();
        }

        var filter = new InternalItemsQuery(user);

        Assert.Equal(1, _service.GetTotalCount(filter, loneSeriesId));
        Assert.Equal(0, _service.GetPlayedCount(filter, loneSeriesId));
        Assert.Equal((0, 1), _service.GetPlayedAndTotalCount(filter, loneSeriesId));
    }

    [Fact]
    public void GetChildCountBatch_MergedFolders_CountsDistinctChildKeys()
    {
        var seriesA = Guid.NewGuid();
        var seriesB = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            foreach (var id in new[] { seriesA, seriesB })
            {
                var series = CreateItem(id);
                series.PresentationUniqueKey = "merged-series";
                context.BaseItems.Add(series);
            }

            // Each folder-item holds a "Season 1"; those two share a key and are one season to the user.
            var sharedSeasonA = CreateItem(Guid.NewGuid(), seriesA);
            sharedSeasonA.PresentationUniqueKey = "merged-series-001";
            var sharedSeasonB = CreateItem(Guid.NewGuid(), seriesB);
            sharedSeasonB.PresentationUniqueKey = "merged-series-001";
            var ownSeason = CreateItem(Guid.NewGuid(), seriesB);
            ownSeason.PresentationUniqueKey = "merged-series-002";

            context.BaseItems.AddRange(sharedSeasonA, sharedSeasonB, ownSeason);
            context.SaveChanges();
        }

        var result = _service.GetChildCountBatch([seriesA, seriesB], null);

        Assert.Equal(2, result[seriesA]);
        Assert.Equal(2, result[seriesB]);
    }

    [Fact]
    public void GetChildCountBatch_FlatSeriesStructure_CountsEpisodesUnderTheirSeason()
    {
        var (seriesId, seasonId) = SeedSeries(flat: true, virtualEpisodes: false);

        var result = _service.GetChildCountBatch([seriesId, seasonId], null);

        Assert.Equal(2, result[seasonId]);

        // The series holds the season, not the episodes: counting those here would double them up.
        Assert.Equal(1, result[seriesId]);
    }

    [Fact]
    public void GetChildCountBatch_SeasonFolderStructure_CountsEachEpisodeOnce()
    {
        var (seriesId, seasonId) = SeedSeries(flat: false, virtualEpisodes: false);

        var result = _service.GetChildCountBatch([seriesId, seasonId], null);

        Assert.Equal(2, result[seasonId]);
        Assert.Equal(1, result[seriesId]);
    }

    [Fact]
    public void GetChildCountBatch_MissingEpisodes_CountedUnlessTheUserHidesThem()
    {
        var (_, seasonId) = SeedSeries(flat: false, virtualEpisodes: true);
        var user = new User("count-test", "provider", "reset");

        user.DisplayMissingEpisodes = true;
        Assert.Equal(2, _service.GetChildCountBatch([seasonId], user)[seasonId]);

        // Nothing this user can open, so nothing to report.
        user.DisplayMissingEpisodes = false;
        Assert.Equal(0, _service.GetChildCountBatch([seasonId], user)[seasonId]);
    }

    [Fact]
    public void GetChildCountBatch_NoUser_CountsMissingEpisodes()
    {
        var (_, seasonId) = SeedSeries(flat: false, virtualEpisodes: true);

        Assert.Equal(2, _service.GetChildCountBatch([seasonId], null)[seasonId]);
    }

    private (Guid SeriesId, Guid SeasonId) SeedSeries(bool flat, bool virtualEpisodes)
    {
        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();

        using var context = CreateDbContext();
        context.BaseItems.Add(CreateItem(seriesId));
        context.BaseItems.Add(CreateItem(seasonId, seriesId));

        // Flat: the episodes sit in the series folder, so ParentId points at the series and only
        // SeasonId ties them to the season they belong to.
        for (var i = 0; i < 2; i++)
        {
            var episode = CreateItem(Guid.NewGuid(), flat ? seriesId : seasonId);
            episode.Type = "MediaBrowser.Controller.Entities.TV.Episode";
            episode.IsFolder = false;
            episode.IsVirtualItem = virtualEpisodes;
            episode.SeasonId = seasonId;
            context.BaseItems.Add(episode);
        }

        context.SaveChanges();

        return (seriesId, seasonId);
    }

    private (User User, Guid SeriesA, Guid SeriesB) SeedMergedSeries(out Guid playedLeafId)
    {
        var user = new User("count-test", "provider", "reset");
        var seriesA = Guid.NewGuid();
        var seriesB = Guid.NewGuid();
        var leafA = Guid.NewGuid();
        var leafB = Guid.NewGuid();
        playedLeafId = leafA;

        using (var context = CreateDbContext())
        {
            context.Users.Add(user);

            foreach (var id in new[] { seriesA, seriesB })
            {
                var series = CreateItem(id);
                series.PresentationUniqueKey = "merged-series";
                context.BaseItems.Add(series);
            }

            context.BaseItems.AddRange(CreateLeaf(leafA), CreateLeaf(leafB));
            context.SaveChanges();

            AddAncestor(context, leafA, seriesA);
            AddAncestor(context, leafB, seriesB);

            context.UserData.Add(new UserData
            {
                ItemId = leafA,
                UserId = user.Id,
                CustomDataKey = string.Empty,
                Played = true,
                Item = null,
                User = null
            });

            context.SaveChanges();
        }

        return (user, seriesA, seriesB);
    }

    private static void AddAncestor(JellyfinDbContext context, Guid itemId, Guid parentItemId)
    {
        context.AncestorIds.Add(new AncestorId
        {
            ItemId = itemId,
            ParentItemId = parentItemId,
            Item = null!,
            ParentItem = null!
        });
    }

    private static BaseItemEntity CreateLeaf(Guid id)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = "Episode",
            IsFolder = false,
            IsVirtualItem = false,
            PresentationUniqueKey = id.ToString("N")
        };
    }

    private static BaseItemEntity CreateItem(Guid id, Guid? parentId = null)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = "Folder",
            ParentId = parentId,
            IsFolder = true
        };
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(
                _applicationPaths,
                NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
