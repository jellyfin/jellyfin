using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Model.Tests.Dlna;

public class StreamBuilderManifestContainerTests
{
    [Theory]
    // A manifest describes a stream instead of carrying one, so it can never be direct played,
    // even when the client claims to support the container.
    [InlineData("hls")]
    [InlineData("hls,applehttp")]
    [InlineData("applehttp")]
    [InlineData("dash")]
    public void GetOptimalVideoStream_ManifestContainer_DoesNotDirectPlay(string container)
    {
        var streamInfo = BuildFor(container);

        Assert.NotNull(streamInfo);
        Assert.Equal(PlayMethod.Transcode, streamInfo.PlayMethod);
    }

    [Fact]
    public void GetOptimalVideoStream_ByteStreamContainer_StillDirectPlays()
    {
        var streamInfo = BuildFor("mp4");

        Assert.NotNull(streamInfo);
        Assert.Equal(PlayMethod.DirectPlay, streamInfo.PlayMethod);
    }

    private static StreamInfo? BuildFor(string container)
    {
        var mediaSource = new MediaSourceInfo
        {
            Id = "test-source",
            Path = "http://example.com/live/channel",
            Protocol = MediaProtocol.Http,
            Container = container,
            SupportsDirectPlay = true,
            SupportsDirectStream = true,
            SupportsTranscoding = true,
            IsInfiniteStream = true,
            IsRemote = true,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Index = 0, Codec = "h264" },
                new MediaStream { Type = MediaStreamType.Audio, Index = 1, Codec = "aac" }
            ]
        };

        var profile = new DeviceProfile
        {
            Name = "Manifest aware client",
            DirectPlayProfiles =
            [
                new DirectPlayProfile
                {
                    Type = DlnaProfileType.Video,
                    Container = "mp4,hls,applehttp,dash",
                    VideoCodec = "h264",
                    AudioCodec = "aac"
                }
            ],
            TranscodingProfiles =
            [
                new TranscodingProfile
                {
                    Type = DlnaProfileType.Video,
                    Context = EncodingContext.Streaming,
                    Protocol = MediaStreamProtocol.hls,
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "aac"
                }
            ]
        };

        var options = new MediaOptions
        {
            ItemId = new Guid("11D229B7-2D48-4B95-9F9B-49F6AB75E613"),
            MediaSourceId = mediaSource.Id,
            MediaSources = [mediaSource],
            DeviceId = "test-deviceId",
            Profile = profile,
            AllowAudioStreamCopy = true,
            AllowVideoStreamCopy = true,
            EnableDirectStream = false // This is disabled in server
        };

        var transcodeSupport = new Mock<ITranscoderSupport>();

        return new StreamBuilder(transcodeSupport.Object, new NullLogger<StreamBuilderManifestContainerTests>())
            .GetOptimalVideoStream(options);
    }
}
