using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Model.Tests.Dlna;

public sealed class PlaybackAccessUrlTests
{
    [Fact]
    public void PlaybackUrlCarriesItsGrantAndSuppressesAccountCredentials()
    {
        var stream = CreateStream();
        stream.PlaybackToken = "playback";
        var url = stream.ToUrl("https://server", "administrator-token", null);

        Assert.Contains("/master.m3u8?", url, StringComparison.Ordinal);
        Assert.Contains("PlaybackToken=playback", url, StringComparison.Ordinal);
        Assert.Contains("PlaySessionId=session", url, StringComparison.Ordinal);
        Assert.Contains("MediaSourceId=source", url, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiKey=", url, StringComparison.Ordinal);
        Assert.DoesNotContain("administrator-token", url, StringComparison.Ordinal);
    }

    [Fact]
    public void AccountPlaybackKeepsItsExistingCredentialFormat()
    {
        var url = CreateStream().ToUrl("https://server", "account", null);
        Assert.Contains("ApiKey=account", url, StringComparison.Ordinal);
        Assert.DoesNotContain("PlaybackToken=", url, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaybackCredentialCannotInjectAdditionalQueryParameters()
    {
        var stream = CreateStream();
        stream.PlaybackToken = "token&ApiKey=account";
        var url = stream.ToUrl("https://server", null, null);
        Assert.Contains("PlaybackToken=token%26ApiKey%3Daccount", url, StringComparison.Ordinal);
    }

    private static StreamInfo CreateStream()
        => new()
        {
            ItemId = Guid.NewGuid(),
            DeviceProfile = new DeviceProfile(),
            MediaType = DlnaProfileType.Video,
            SubProtocol = MediaStreamProtocol.hls,
            MediaSource = new MediaSourceInfo { Id = "source" },
            DeviceId = "renderer",
            PlaySessionId = "session"
        };
}
