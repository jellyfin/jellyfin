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

        [Theory]
        [InlineData(0, 96, "", true)]
        [InlineData(0, 84, "", false)]
        [InlineData(0, 0, "", false)]
        [InlineData(0, 96, "[matroska,webm @ 0x1] File ended prematurely", false)]
        [InlineData(0, 96, "[in#0/matroska,webm @ 0x1] [warning] File ended prematurely", false)]
        [InlineData(1, 84, "[matroska,webm @ 0x1] File ended prematurely", false)]
        [InlineData(1, 84, "Invalid data found when processing input", false)]
        [InlineData(1, 84, "[in#0 @ 0x1] Error opening input: Invalid data found when processing input", false)]
        [InlineData(1, 84, "Error while decoding stream #0:0: Invalid data found when processing input", false)]
        [InlineData(1, 84, "[vdec#0:0/h264 @ 0x1] Error submitting packet to decoder: Invalid data found when processing input", false)]
        [InlineData(1, 84, "Error during demuxing: Invalid data found when processing input", false)]
        [InlineData(1, 84, "[vist#0:0/h264 @ 0x9d140000] [dec:h264 @ 0x9cc40140] Error processing packet in decoder: Invalid data found when processing input", false)]
        [InlineData(1, 84, "[vist#0:0/h264 @ 0x1] [dec:h264 @ 0x2] Error processing packet in decoder: Cannot allocate memory", null)]
        [InlineData(1, 84, "[vist#0:0/h264 @ 0x1] [dec:h264 @ 0x2] Task finished with error code: -1094995529 (Invalid data found when processing input)", null)]
        [InlineData(1, 12, "[h264 @ 0x1] corrupt decoded frame", false)]
        [InlineData(1, 12, "Packet corrupt (stream = 0, dts = 13)", false)]
        [InlineData(-1, null, "timed out after 60 s", null)]
        [InlineData(-1, null, "ffmpeg did not start within 30 s", null)]
        [InlineData(1, 0, "Cannot allocate memory", null)]
        [InlineData(1, 0, "Decoder not found", null)]
        [InlineData(1, 0, "OpenEncodeSessionEx failed: out of memory (10)", null)]
        [InlineData(0, 96, "Input #0, matroska,webm, from '/Volumes/Packet corrupt/cache/h264.mkv':", true)]
        [InlineData(0, 96, "[in#0 @ 0x1] [verbose] Opening an input file '/Volumes/File ended prematurely/cache/h264.mkv'.", true)]
        [InlineData(0, 96, "Metadata:\n  title : Packet corrupt\n  comment : Invalid data found when processing input", true)]
        [InlineData(0, 96, "Metadata:\n  title : File ended prematurely\n  comment : corrupt decoded frame", true)]
        [InlineData(1, 0, "[in#0 @ 0x1] Opening input file '/Volumes/Packet corrupt/cache/h264.mkv'.", null)]
        [InlineData(0, null, "", null)]
        public void IsCompleteSoftwareDecode_OnlyProvenSourceDamageInvalidatesTheCache(int exitCode, int? frames, string output, bool? expected)
            => Assert.Equal(expected, TranscodeTestClipValidation.IsCompleteSoftwareDecode(exitCode, frames, output));

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
