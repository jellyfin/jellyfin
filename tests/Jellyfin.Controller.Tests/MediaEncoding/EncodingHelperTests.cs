using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding
{
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

            // Different file from ext1, so in-file index is 0
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

            // Second track in the same .mks file → in-file index 1
            Assert.Contains("-map 1:1", args, StringComparison.Ordinal);
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
}
