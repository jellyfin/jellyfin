using System;
using Emby.Server.Implementations.Images;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Images;

/// <summary>
/// A generated image is made again when the file behind it has changed. A stored timestamp can
/// differ from the file time by less than a second without the file having changed.
/// </summary>
public sealed class BaseDynamicImageProviderTests
{
    private const string ImagePath = "/metadata/playlists/playlist/poster.png";

    private static readonly DateTime _fileTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1234567);

    [Fact]
    public void HasChangedByDate_StoredTimeTruncatedToMicroseconds_ReturnsFalse()
    {
        var storedTime = _fileTime.AddTicks(-(_fileTime.Ticks % 10));

        Assert.NotEqual(_fileTime, storedTime);
        Assert.False(HasChangedByDate(storedTime));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(TimeSpan.TicksPerMillisecond)]
    [InlineData(-TimeSpan.TicksPerMillisecond)]
    [InlineData(TimeSpan.TicksPerSecond)]
    [InlineData(-TimeSpan.TicksPerSecond)]
    public void HasChangedByDate_StoredTimeWithinOneSecond_ReturnsFalse(long ticks)
    {
        Assert.False(HasChangedByDate(_fileTime.AddTicks(ticks)));
    }

    [Theory]
    [InlineData(TimeSpan.TicksPerSecond + 1)]
    [InlineData(-(TimeSpan.TicksPerSecond + 1))]
    [InlineData(10 * TimeSpan.TicksPerMinute)]
    [InlineData(-10 * TimeSpan.TicksPerMinute)]
    public void HasChangedByDate_StoredTimeMoreThanOneSecondApart_ReturnsTrue(long ticks)
    {
        Assert.True(HasChangedByDate(_fileTime.AddTicks(ticks)));
    }

    [Fact]
    public void HasChangedByDate_ImageWithoutPath_ReturnsFalse()
    {
        var provider = new TestablePlaylistImageProvider(Mock.Of<IFileSystem>());

        Assert.False(provider.HasChangedByDate(new Playlist(), new ItemImageInfo { Path = string.Empty, DateModified = _fileTime }));
    }

    private static bool HasChangedByDate(DateTime storedTime)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.GetLastWriteTimeUtc(ImagePath)).Returns(_fileTime);

        var provider = new TestablePlaylistImageProvider(fileSystem.Object);

        return provider.HasChangedByDate(new Playlist(), new ItemImageInfo { Path = ImagePath, DateModified = storedTime });
    }

    private sealed class TestablePlaylistImageProvider : PlaylistImageProvider
    {
        public TestablePlaylistImageProvider(IFileSystem fileSystem)
            : base(fileSystem, Mock.Of<IProviderManager>(), Mock.Of<IApplicationPaths>(), Mock.Of<IImageProcessor>())
        {
        }

        public new bool HasChangedByDate(BaseItem item, ItemImageInfo image) => base.HasChangedByDate(item, image);
    }
}
