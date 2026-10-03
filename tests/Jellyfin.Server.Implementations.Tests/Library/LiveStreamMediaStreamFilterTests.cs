using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Library;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class LiveStreamMediaStreamFilterTests
{
    [Fact]
    public void FilterProbedStreams_MpegTsWithDvbsub_KeepsDataVideoAudioAndSubtitles()
    {
        var streams = new List<MediaStream>
        {
            CreateStream(MediaStreamType.Data, 0, "epg"),
            CreateStream(MediaStreamType.Video, 1, "h264"),
            CreateStream(MediaStreamType.Audio, 2, "ac3"),
            new()
            {
                Index = 3,
                Type = MediaStreamType.Subtitle,
                Codec = "DVBSUB",
                Language = "dut",
                IsHearingImpaired = true,
            },
            CreateStream(MediaStreamType.Audio, 4, "aac", "eng"),
            CreateStream(MediaStreamType.Video, 5, "mpeg2"),
            CreateStream(MediaStreamType.Subtitle, 6, "DVBSUB", "eng"),
        };

        var filtered = LiveStreamMediaStreamFilter.FilterProbedStreams(streams);

        Assert.Equal(5, filtered.Count);
        Assert.Equal([0, 1, 2, 3, 6], filtered.Select(i => i.Index));
        Assert.Equal(2, filtered.Count(i => i.Type == MediaStreamType.Subtitle));
        Assert.Single(filtered, i => i.Type == MediaStreamType.Video);
        Assert.Single(filtered, i => i.Type == MediaStreamType.Audio);
    }

    [Fact]
    public void FilterProbedStreams_ExcludesDvbtxtSubtitle()
    {
        var streams = new List<MediaStream>
        {
            CreateStream(MediaStreamType.Video, 0, "h264"),
            CreateStream(MediaStreamType.Audio, 1, "mp2", "rum"),
            CreateStream(MediaStreamType.Subtitle, 2, "DVBTXT", "rum"),
            CreateStream(MediaStreamType.Subtitle, 3, "DVBSUB", "rum"),
        };

        var filtered = LiveStreamMediaStreamFilter.FilterProbedStreams(streams);

        Assert.Equal(3, filtered.Count);
        Assert.DoesNotContain(filtered, i => i.Codec == "DVBTXT");
        Assert.Single(filtered, i => i.Type == MediaStreamType.Subtitle);
    }

    [Fact]
    public void FilterProbedStreams_SkipsZeroChannelAudio()
    {
        var streams = new List<MediaStream>
        {
            CreateStream(MediaStreamType.Video, 0, "h264"),
            new()
            {
                Index = 1,
                Type = MediaStreamType.Audio,
                Codec = "mp3",
                Channels = 0,
            },
            new()
            {
                Index = 2,
                Type = MediaStreamType.Audio,
                Codec = "mp2",
                Channels = 2,
                Language = "eng",
            },
            CreateStream(MediaStreamType.Subtitle, 3, "DVBSUB", "eng"),
        };

        var filtered = LiveStreamMediaStreamFilter.FilterProbedStreams(streams);

        Assert.Single(filtered, i => i.Type == MediaStreamType.Audio);
        Assert.Equal(2, filtered.Single(i => i.Type == MediaStreamType.Audio).Index);
    }

    [Fact]
    public void FilterProbedStreams_ExcludesTeletextCodecName()
    {
        var streams = new List<MediaStream>
        {
            CreateStream(MediaStreamType.Video, 0, "h264"),
            CreateStream(MediaStreamType.Audio, 1, "mp2"),
            CreateStream(MediaStreamType.Subtitle, 2, "dvb_teletext", "rum"),
            CreateStream(MediaStreamType.Subtitle, 3, "DVBSUB", "rum"),
        };

        var filtered = LiveStreamMediaStreamFilter.FilterProbedStreams(streams);

        Assert.Equal(3, filtered.Count);
        Assert.DoesNotContain(filtered, i => i.Codec == "dvb_teletext");
        Assert.Single(filtered, i => i.Type == MediaStreamType.Subtitle);
    }

    [Fact]
    public void FilterProbedStreams_PreservesOriginalStreamIndices()
    {
        var streams = new List<MediaStream>
        {
            CreateStream(MediaStreamType.Data, 0, "epg"),
            CreateStream(MediaStreamType.Video, 1, "h264"),
            CreateStream(MediaStreamType.Audio, 2, "ac3"),
            CreateStream(MediaStreamType.Subtitle, 3, "DVBSUB", "rum"),
        };

        var filtered = LiveStreamMediaStreamFilter.FilterProbedStreams(streams);

        Assert.Equal([0, 1, 2, 3], filtered.Select(i => i.Index));
    }

    private static MediaStream CreateStream(
        MediaStreamType type,
        int index,
        string codec,
        string? language = null)
    {
        return new MediaStream
        {
            Type = type,
            Index = index,
            Codec = codec,
            Language = language,
            Channels = type == MediaStreamType.Audio ? 2 : null,
        };
    }
}
