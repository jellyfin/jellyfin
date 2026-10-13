using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public sealed class HarmonizeConflictingUserDataTests : DatabaseTestFixture
{
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _itemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _untouchedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Perform_ConflictingRows_CollapsesToMostRecentPlay()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });
            ctx.BaseItems.Add(new BaseItemEntity { Id = _itemId, Type = typeof(Folder).FullName! });
            ctx.BaseItems.Add(new BaseItemEntity { Id = _untouchedId, Type = typeof(Folder).FullName! });

            // One item holding rows from two different eras of itself...
            ctx.UserData.Add(Row(_itemId, "497698", new DateTime(2023, 8, 14, 0, 0, 0, DateTimeKind.Utc), playCount: 9, positionTicks: 0, played: true));
            ctx.UserData.Add(Row(_itemId, "tt3480822", new DateTime(2021, 12, 31, 0, 0, 0, DateTimeKind.Utc), playCount: 7, positionTicks: 490));

            // ...and one whose rows already agree.
            ctx.UserData.Add(Row(_untouchedId, "121", new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), playCount: 2, positionTicks: 77));
            ctx.UserData.Add(Row(_untouchedId, "tt0167261", new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), playCount: 2, positionTicks: 77));

            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            var rows = ctx.UserData.Where(e => e.ItemId.Equals(_itemId)).ToList();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row =>
            {
                Assert.True(row.Played);
                Assert.Equal(0, row.PlaybackPositionTicks);
                Assert.Equal(9, row.PlayCount);
            });

            Assert.All(ctx.UserData.Where(e => e.ItemId.Equals(_untouchedId)), row => Assert.Equal(77, row.PlaybackPositionTicks));
        }
    }

    [Fact]
    public async Task Perform_DetachedRows_AreLeftAlone()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });

            // Detached rows belong to no item, so they are not a conflict and must keep their state
            // for the next item that claims one of their keys.
            ctx.UserData.Add(Row(BaseItemRepository.PlaceholderId, "497698", new DateTime(2023, 8, 14, 0, 0, 0, DateTimeKind.Utc), playCount: 9, positionTicks: 0, played: true));
            ctx.UserData.Add(Row(BaseItemRepository.PlaceholderId, "tt3480822", new DateTime(2021, 12, 31, 0, 0, 0, DateTimeKind.Utc), playCount: 7, positionTicks: 490));

            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            var rows = ctx.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId)).OrderBy(e => e.CustomDataKey).ToList();
            Assert.Equal(0, rows[0].PlaybackPositionTicks);
            Assert.Equal(490, rows[1].PlaybackPositionTicks);
        }
    }

    private UserData Row(Guid itemId, string key, DateTime lastPlayed, int playCount, long positionTicks, bool played = false)
    {
        return new UserData
        {
            ItemId = itemId,
            Item = null,
            UserId = _userId,
            User = null,
            CustomDataKey = key,
            LastPlayedDate = lastPlayed,
            PlayCount = playCount,
            PlaybackPositionTicks = positionTicks,
            Played = played
        };
    }

    private HarmonizeConflictingUserData CreateMigration()
    {
        return new HarmonizeConflictingUserData(
            new StartupLogger<HarmonizeConflictingUserData>(NullLogger<HarmonizeConflictingUserData>.Instance),
            CreateDbContextFactory());
    }
}
