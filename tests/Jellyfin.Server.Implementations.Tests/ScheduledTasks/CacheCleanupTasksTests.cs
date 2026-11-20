using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.AppBase;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ScheduledTasks;

// The transcode folder holds a marker file and a CACHEDIR.TAG that the server only creates at startup.
// The cleanup tasks must leave them alone, otherwise the next request has to recreate the marker and
// can race with another request doing the same.
public sealed class CacheCleanupTasksTests : IDisposable
{
    private static readonly DateTime _old = DateTime.UtcNow.AddDays(-60);

    private readonly string _root;
    private readonly string _transcodePath;

    public CacheCleanupTasksTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-cache-tasks-tests-" + Guid.NewGuid());
        _transcodePath = Path.Combine(_root, "transcodes");
        Directory.CreateDirectory(_transcodePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public async Task DeleteCacheFileTask_KeepsMarkerAndCacheTag_DeletesOldFiles()
    {
        var (marker, cacheTag, segment) = CreateOldFiles();
        var appPaths = new Mock<IApplicationPaths>();
        appPaths.SetupGet(p => p.CachePath).Returns(_root);
        appPaths.SetupGet(p => p.TempDirectory).Returns(Path.Combine(_root, "temp-missing"));

        var task = new DeleteCacheFileTask(
            appPaths.Object,
            NullLogger<DeleteCacheFileTask>.Instance,
            CreateFileSystem(),
            Mock.Of<ILocalizationManager>());

        await task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        AssertOnlySegmentDeleted(marker, cacheTag, segment);
    }

    [Fact]
    public async Task DeleteTranscodeFileTask_KeepsMarkerAndCacheTag_DeletesOldFiles()
    {
        var (marker, cacheTag, segment) = CreateOldFiles();
        var config = new Mock<IConfigurationManager>();
        config.Setup(c => c.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = _transcodePath });
        config.SetupGet(c => c.CommonApplicationPaths).Returns(Mock.Of<IApplicationPaths>());

        var task = new DeleteTranscodeFileTask(
            NullLogger<DeleteTranscodeFileTask>.Instance,
            CreateFileSystem(),
            config.Object,
            Mock.Of<ILocalizationManager>());

        await task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        AssertOnlySegmentDeleted(marker, cacheTag, segment);
    }

    [Fact]
    public async Task CreateAndCheckMarker_ConcurrentCalls_DoNotThrow()
    {
        var paths = new TestApplicationPaths(_root);

        for (var round = 0; round < 50; round++)
        {
            var dir = Path.Combine(_root, "round-" + round);
            var tasks = new List<Task>();
            for (var i = 0; i < 16; i++)
            {
                tasks.Add(Task.Run(() => paths.CreateAndCheckMarker(dir, "transcode"), TestContext.Current.CancellationToken));
            }

            await Task.WhenAll(tasks);
            Assert.True(File.Exists(Path.Combine(dir, ".jellyfin-transcode")));
        }
    }

    private static ManagedFileSystem CreateFileSystem()
        => new(NullLogger<ManagedFileSystem>.Instance, Mock.Of<IApplicationPaths>(), Array.Empty<IShortcutHandler>());

    private (string Marker, string CacheTag, string Segment) CreateOldFiles()
    {
        var marker = Path.Combine(_transcodePath, ".jellyfin-transcode");
        var cacheTag = Path.Combine(_transcodePath, "CACHEDIR.TAG");
        var segment = Path.Combine(_transcodePath, "segment0.ts");
        foreach (var file in new[] { marker, cacheTag, segment })
        {
            File.WriteAllText(file, "x");
            File.SetLastWriteTimeUtc(file, _old);
        }

        return (marker, cacheTag, segment);
    }

    private static void AssertOnlySegmentDeleted(string marker, string cacheTag, string segment)
    {
        Assert.True(File.Exists(marker));
        Assert.True(File.Exists(cacheTag));
        Assert.False(File.Exists(segment));
    }

    private sealed class TestApplicationPaths : BaseApplicationPaths
    {
        public TestApplicationPaths(string root)
            : base(root, root, root, root, root)
        {
        }
    }
}
