using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperTests
{
    [Fact]
    public void GetMapArgs_NoSubtitle_ExcludesAllSubs()
    {
        var state = BuildState(subtitle: null, deliveryMethod: null);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map -0:s", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-map 1:", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_InternalSrt_MapsFromPrimaryInput()
    {
        var sub = new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "srt" };
        var state = BuildState(sub, SubtitleDeliveryMethod.Embed);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:2", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-map 1:", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_InternalSubAtHigherIndex_MapsCorrectIndex()
    {
        var sub0 = new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "srt" };
        var sub1 = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "ass" };
        var state = BuildState(sub1, SubtitleDeliveryMethod.Embed, additionalStreams: [sub0, sub1]);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:3", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_ExternalSrt_MapsFirstStreamFromInput1()
    {
        var sub = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.en.srt"
        };
        var state = BuildState(sub, SubtitleDeliveryMethod.Embed);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 1:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_SecondExternalSrt_StillMaps1Colon0()
    {
        // Two separate .srt files — selecting the second one still maps 1:0
        // because Jellyfin feeds only the selected file as ffmpeg input 1.
        var ext1 = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.en.srt"
        };
        var ext2 = new MediaStream
        {
            Index = 3,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.fr.srt"
        };
        var state = BuildState(ext2, SubtitleDeliveryMethod.Embed, additionalStreams: [ext1, ext2]);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 1:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_MksFirstTrack_MapsInFileIndex0()
    {
        var mks0 = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "subrip",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var mks1 = new MediaStream
        {
            Index = 3,
            Type = MediaStreamType.Subtitle,
            Codec = "ass",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var state = BuildState(mks0, SubtitleDeliveryMethod.Embed, additionalStreams: [mks0, mks1]);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 1:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_MksSecondTrack_MapsInFileIndex1()
    {
        var mks0 = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "subrip",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var mks1 = new MediaStream
        {
            Index = 3,
            Type = MediaStreamType.Subtitle,
            Codec = "ass",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var mks2 = new MediaStream
        {
            Index = 4,
            Type = MediaStreamType.Subtitle,
            Codec = "subrip",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var state = BuildState(mks1, SubtitleDeliveryMethod.Embed, additionalStreams: [mks0, mks1, mks2]);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 1:1", args, StringComparison.Ordinal);
    }

    [Fact]
    public void CanStreamCopyVideo_AlwaysBurnSubtitleWhenEncodingVideo_ReturnsFalse()
    {
        var sub = new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "srt" };
        var state = BuildState(sub, SubtitleDeliveryMethod.External);
        state.BaseRequest.SubtitleStreamIndex = sub.Index;
        state.BaseRequest.AlwaysBurnInSubtitleWhenTranscoding = true;
        state.OutputVideoCodec = "h264";
        state.SupportedVideoCodecs = ["h264"];

        Assert.False(CreateHelper().CanStreamCopyVideo(state, state.VideoStream));
    }

    [Theory]
    [InlineData(SubtitleDeliveryMethod.Embed, true, "movie.idx")]
    [InlineData(SubtitleDeliveryMethod.Encode, true, "movie.idx")]
    [InlineData(SubtitleDeliveryMethod.Embed, false, "movie.sub")]
    [InlineData(SubtitleDeliveryMethod.Encode, false, "movie.sub")]
    public void GetInputArgument_VobSub_UsesCorrectPath(
        SubtitleDeliveryMethod deliveryMethod,
        bool createIdxFile,
        string expectedFilename)
    {
        var tempDir = Directory.CreateTempSubdirectory("jellyfin-test-");
        try
        {
            var subFile = Path.Combine(tempDir.FullName, "movie.sub");
            File.WriteAllText(subFile, "dummy");

            if (createIdxFile)
            {
                File.WriteAllText(Path.Combine(tempDir.FullName, "movie.idx"), "dummy");
            }

            var sub = new MediaStream
            {
                Index = 2,
                Type = MediaStreamType.Subtitle,
                Codec = "dvdsub",
                IsExternal = true,
                SupportsExternalStream = true,
                Path = subFile
            };
            var state = BuildState(sub, deliveryMethod);
            var inputArgs = CreateHelper().GetInputArgument(state, new EncodingOptions(), null);

            Assert.Contains(expectedFilename, inputArgs, StringComparison.Ordinal);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Theory]
    [InlineData("aac", 44100, 44100)] // non-opus: requested rate must be preserved (issue #17026)
    [InlineData("aac", 48000, 48000)]
    [InlineData("mp3", 22050, 22050)]
    [InlineData("flac", 96000, 96000)]
    [InlineData("opus", 44100, 48000)] // opus: must snap to a libopus-supported rate
    [InlineData("opus", 22050, 24000)]
    [InlineData("opus", 8000, 8000)]
    public void GetProgressiveAudioFullCommandLine_SampleRate_OnlyClampedForOpus(
        string audioCodec,
        int requestedSampleRate,
        int expectedSampleRate)
    {
        var state = BuildAudioState(audioCodec, requestedSampleRate);
        var args = CreateHelper().GetProgressiveAudioFullCommandLine(state, new EncodingOptions(), "/tmp/out");

        Assert.Contains("-ar " + expectedSampleRate, args, StringComparison.Ordinal);
    }

    private static EncodingJobInfo BuildAudioState(string audioCodec, int requestedSampleRate)
    {
        var audio = new MediaStream { Index = 0, Type = MediaStreamType.Audio, Codec = "flac", SampleRate = 96000 };

        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "flac",
                MediaStreams = new List<MediaStream> { audio },
                Path = "/media/track.flac",
                Protocol = MediaProtocol.File,
            },
            AudioStream = audio,
            OutputAudioCodec = audioCodec,
            BaseRequest = new VideoRequestDto
            {
                AudioCodec = audioCodec,
                AudioSampleRate = requestedSampleRate,
            },
            IsVideoRequest = false,
            IsInputVideo = false,
        };
    }

    private static EncodingJobInfo BuildState(
        MediaStream? subtitle,
        SubtitleDeliveryMethod? deliveryMethod,
        MediaStream[]? additionalStreams = null)
    {
        var video = new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "aac" };
        var streams = new List<MediaStream> { video, audio };

        if (additionalStreams is not null)
        {
            streams.AddRange(additionalStreams);
        }
        else if (subtitle is not null)
        {
            streams.Add(subtitle);
        }

        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "mkv",
                MediaStreams = streams,
            },
            VideoStream = video,
            AudioStream = audio,
            SubtitleStream = subtitle,
            SubtitleDeliveryMethod = deliveryMethod ?? SubtitleDeliveryMethod.Drop,
            BaseRequest = new VideoRequestDto(),
            IsVideoRequest = true,
            IsInputVideo = true,
        };
    }

    private static EncodingHelper CreateHelper()
    {
        var appPaths = Mock.Of<IApplicationPaths>();
        var mediaEncoder = new Mock<IMediaEncoder>();
        var subtitleEncoder = new Mock<ISubtitleEncoder>();
        var config = new Mock<IConfiguration>();
        var configurationManager = new Mock<IConfigurationManager>();
        var pathManager = new Mock<IPathManager>();

        return new EncodingHelper(
            appPaths,
            mediaEncoder.Object,
            subtitleEncoder.Object,
            config.Object,
            configurationManager.Object,
            pathManager.Object);
    }
}
