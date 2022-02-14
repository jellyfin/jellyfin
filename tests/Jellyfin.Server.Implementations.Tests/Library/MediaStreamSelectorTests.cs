using System;
using Emby.Server.Implementations.Library;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class MediaStreamSelectorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetDefaultAudioStreamIndex_EmptyStreams_Null(bool preferDefaultTrack)
    {
        Assert.Null(MediaStreamSelector.GetDefaultAudioStreamIndex(Array.Empty<MediaStream>(), Array.Empty<string>(), preferDefaultTrack));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetDefaultAudioStreamIndex_WithoutDefault_NotNull(bool preferDefaultTrack)
    {
        var streams = new[]
        {
            new MediaStream()
        };

        Assert.NotNull(MediaStreamSelector.GetDefaultAudioStreamIndex(streams, Array.Empty<string>(), preferDefaultTrack));
    }
}
