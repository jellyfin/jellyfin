using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.MediaSegments;
using MediaBrowser.Controller.MediaSegments;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MediaSegments;

public class MediaSegmentManagerTests
{
    [Fact]
    public void ApplyProviderOverrides_KeepsAllOverridesAndUnrelatedTypes()
    {
        const string overridingProvider = "manual";
        var manualIntro1 = CreateSegment(overridingProvider, MediaSegmentType.Intro, 10);
        var manualIntro2 = CreateSegment(overridingProvider, MediaSegmentType.Intro, 30);
        var automaticIntro = CreateSegment("automatic", MediaSegmentType.Intro, 12);
        var automaticRecap = CreateSegment("automatic", MediaSegmentType.Recap, 1);

        var result = MediaSegmentManager.ApplyProviderOverrides(
                [manualIntro1, manualIntro2, automaticIntro, automaticRecap],
                new HashSet<string> { overridingProvider })
            .ToArray();

        Assert.Equal(3, result.Length);
        Assert.Contains(manualIntro1, result);
        Assert.Contains(manualIntro2, result);
        Assert.Contains(automaticRecap, result);
        Assert.DoesNotContain(automaticIntro, result);
    }

    [Fact]
    public async Task GetSegmentedItemCounts_CountsDistinctVideoItemsInOneQuery()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(connection)
            .Options;
        JellyfinDbContext CreateContext()
            => new(
                options,
                NullLogger<JellyfinDbContext>.Instance,
                new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
                new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        var videoId = Guid.NewGuid();
        var secondVideoId = Guid.NewGuid();
        var audioId = Guid.NewGuid();
        await using (var db = CreateContext())
        {
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            db.BaseItems.AddRange(
                CreateItem(videoId, "Video"),
                CreateItem(secondVideoId, "Video"),
                CreateItem(audioId, "Audio"));
            db.MediaSegments.AddRange(
                CreateSegment(videoId, MediaSegmentType.Intro),
                CreateSegment(videoId, MediaSegmentType.Intro),
                CreateSegment(secondVideoId, MediaSegmentType.Recap),
                CreateSegment(audioId, MediaSegmentType.Intro));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory
            .Setup(mock => mock.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateContext);
        var manager = new MediaSegmentManager(
            NullLogger<MediaSegmentManager>.Instance,
            factory.Object,
            Array.Empty<IMediaSegmentProvider>());

        var result = await manager.GetSegmentedItemCountsAsync(
            [MediaSegmentType.Intro, MediaSegmentType.Recap, MediaSegmentType.Outro],
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result[MediaSegmentType.Intro]);
        Assert.Equal(1, result[MediaSegmentType.Recap]);
        Assert.False(result.ContainsKey(MediaSegmentType.Outro));
    }

    private static MediaSegment CreateSegment(string providerId, MediaSegmentType type, int startSeconds)
        => new()
        {
            Id = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            SegmentProviderId = providerId,
            Type = type,
            StartTicks = TimeSpan.FromSeconds(startSeconds).Ticks,
            EndTicks = TimeSpan.FromSeconds(startSeconds + 5).Ticks
        };

    private static MediaSegment CreateSegment(Guid itemId, MediaSegmentType type)
        => new()
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            SegmentProviderId = "provider",
            Type = type,
            StartTicks = 0,
            EndTicks = TimeSpan.FromSeconds(5).Ticks
        };

    private static BaseItemEntity CreateItem(Guid id, string mediaType)
        => new()
        {
            Id = id,
            Type = "Video",
            MediaType = mediaType
        };
}
