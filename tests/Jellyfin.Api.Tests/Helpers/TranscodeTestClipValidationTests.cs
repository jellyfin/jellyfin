using System;
using Jellyfin.Api.Helpers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public class TranscodeTestClipValidationTests
    {
        [Theory]
        [InlineData("h264", 8, false)]
        [InlineData("hevc", 8, false)]
        [InlineData("hevc", 10, false)]
        [InlineData("vp8", 8, false)]
        [InlineData("vp9", 8, false)]
        [InlineData("av1", 10, false)]
        [InlineData("hevc", 10, true)]
        public void IsValid_AcceptsEveryGeneratedClipFormat(string codec, int bitDepth, bool hdr)
            => Assert.True(TranscodeTestClipValidation.IsValid(Clip(codec, bitDepth, hdr), codec, bitDepth, hdr));

        [Theory]
        [InlineData(null, false)]
        [InlineData(-1.0, false)]
        [InlineData(0.0, false)]
        [InlineData(0.5, false)]
        [InlineData(2.0, false)]
        [InlineData(3.95, false)]
        [InlineData(3.959, true)]
        [InlineData(4.0, true)]
        [InlineData(4.041, true)]
        [InlineData(4.05, false)]
        [InlineData(60.0, false)]
        public void IsValid_ProbeableButIncompleteClipsAreNotValid(double? seconds, bool expected)
        {
            var info = Clip("h264", 8, false);
            info.RunTimeTicks = seconds is double duration ? TimeSpan.FromSeconds(duration).Ticks : null;
            Assert.Equal(expected, TranscodeTestClipValidation.IsValid(info, "h264", 8, false));
        }

        [Fact]
        public void IsValid_DurationAllowanceIsAtMostOneFrame()
        {
            var info = Clip("h264", 8, false);
            var frameTicks = TimeSpan.TicksPerSecond / 24;
            info.RunTimeTicks = (4 * TimeSpan.TicksPerSecond) - frameTicks;
            Assert.True(TranscodeTestClipValidation.IsValid(info, "h264", 8, false));
            info.RunTimeTicks--;
            Assert.False(TranscodeTestClipValidation.IsValid(info, "h264", 8, false));
        }

        [Theory]
        [InlineData(8)]
        [InlineData(10)]
        public void IsValid_PixelFormatProvesDepthWhenBitDepthMetadataIsAbsent(int bitDepth)
        {
            var info = Clip("hevc", bitDepth, false);
            info.MediaStreams[0].BitDepth = null;
            Assert.True(TranscodeTestClipValidation.IsValid(info, "hevc", bitDepth, false));
            info.MediaStreams[0].PixelFormat = null!;
            Assert.False(TranscodeTestClipValidation.IsValid(info, "hevc", bitDepth, false));
        }

        [Fact]
        public void IsValid_RejectsMismatchedCodecDepthAndChroma()
        {
            var info = Clip("hevc", 10, false);
            Assert.False(TranscodeTestClipValidation.IsValid(info, "h264", 10, false));
            Assert.False(TranscodeTestClipValidation.IsValid(info, "hevc", 8, false));
            Assert.False(TranscodeTestClipValidation.IsValid(info, "hevc", 12, false));
            info.MediaStreams[0].BitDepth = 8;
            Assert.False(TranscodeTestClipValidation.IsValid(info, "hevc", 10, false));
            info.MediaStreams[0].BitDepth = 10;
            info.MediaStreams[0].PixelFormat = "yuv444p10le";
            Assert.False(TranscodeTestClipValidation.IsValid(info, "hevc", 10, false));
        }

        [Fact]
        public void IsValid_RequiresTheExpectedHdrTags()
        {
            var hdr = Clip("hevc", 10, true);
            Assert.False(TranscodeTestClipValidation.IsValid(hdr, "hevc", 10, false));
            Assert.False(TranscodeTestClipValidation.IsValid(Clip("hevc", 10, false), "hevc", 10, true));
            hdr.MediaStreams[0].ColorTransfer = "arib-std-b67";
            Assert.False(TranscodeTestClipValidation.IsValid(hdr, "hevc", 10, true));
            hdr.MediaStreams[0].ColorTransfer = "smpte2084";
            hdr.MediaStreams[0].ColorPrimaries = null!;
            Assert.False(TranscodeTestClipValidation.IsValid(hdr, "hevc", 10, true));
            hdr.MediaStreams[0].ColorPrimaries = "bt2020";
            hdr.MediaStreams[0].ColorSpace = "bt709";
            Assert.False(TranscodeTestClipValidation.IsValid(hdr, "hevc", 10, true));
        }

        [Fact]
        public void IsValid_RequiresExactlyOneVideoStream()
        {
            var info = Clip("h264", 8, false);
            var video = info.MediaStreams[0];
            info.MediaStreams = [];
            Assert.False(TranscodeTestClipValidation.IsValid(info, "h264", 8, false));
            info.MediaStreams = [video, video];
            Assert.False(TranscodeTestClipValidation.IsValid(info, "h264", 8, false));
            video.Type = MediaStreamType.Audio;
            info.MediaStreams = [video];
            Assert.False(TranscodeTestClipValidation.IsValid(info, "h264", 8, false));
        }

        private static MediaSourceInfo Clip(string codec, int bitDepth, bool hdr)
            => new()
            {
                RunTimeTicks = 4 * TimeSpan.TicksPerSecond,
                MediaStreams =
                [
                    new MediaStream
                    {
                        Type = MediaStreamType.Video,
                        Codec = codec,
                        BitDepth = bitDepth,
                        PixelFormat = bitDepth == 8 ? "yuv420p" : "yuv420p10le",
                        ColorTransfer = hdr ? "smpte2084" : null!,
                        ColorPrimaries = hdr ? "bt2020" : null!,
                        ColorSpace = hdr ? "bt2020nc" : null!
                    }
                ]
            };
    }
}
