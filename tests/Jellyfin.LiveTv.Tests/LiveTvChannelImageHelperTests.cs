using Jellyfin.LiveTv;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.LiveTv.Tests;

public class LiveTvChannelImageHelperTests
{
    [Fact]
    public void UpdateChannelImageIfNeeded_NoSource_DoesNotUpdate()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };

        var updated = LiveTvChannelImageHelper.UpdateChannelImageIfNeeded(channel, null, null);

        Assert.False(updated);
        Assert.False(channel.HasImage(ImageType.Primary));
    }

    [Fact]
    public void UpdateChannelImageIfNeeded_WithUrl_AppliesUrl()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };

        var updated = LiveTvChannelImageHelper.UpdateChannelImageIfNeeded(
            channel,
            null,
            "https://example.com/icon.png");

        Assert.True(updated);
        Assert.True(channel.HasImage(ImageType.Primary));
        Assert.Equal("https://example.com/icon.png", channel.GetImagePath(ImageType.Primary));
    }

    [Fact]
    public void UpdateChannelImageIfNeeded_SameUrl_StillUpdates()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };
        LiveTvChannelImageHelper.UpdateChannelImageIfNeeded(channel, null, "https://example.com/icon.png");

        var updated = LiveTvChannelImageHelper.UpdateChannelImageIfNeeded(
            channel,
            null,
            "https://example.com/icon.png");

        Assert.True(updated);
        Assert.Equal("https://example.com/icon.png", channel.GetImagePath(ImageType.Primary));
    }
}
