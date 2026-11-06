using System;
using System.Globalization;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public class TranscodeTestRulesTests
    {
        // Lines from jellyfin-ffmpeg 8.1 at -v verbose: VideoToolbox decoding, and a fallback to the CPU.
        private const string VideoToolboxOutput = "[graph -1 input from stream 0:0 @ 0xb3302ca80] w:1920 h:1080 pixfmt:videotoolbox_vld tb:1/1000 fr:24/1 sar:1/1";
        private const string SoftwareOutput = "[graph -1 input from stream 0:0 @ 0x9a50486c0] w:1280 h:720 pixfmt:yuv420p tb:1/1000 fr:24/1 sar:1/1 csp:bt470bg";

        // An RTX 3070, which has no AV1 encoder: ffmpeg 8.1 gave no "frames encoded" count.
        private const string Rtx3070Av1Output = "[vost#0:0/av1_nvenc @ 0x7183faefdf00] Task finished with error code: -22 (Invalid argument)\n"
            + "[in#0/matroska,webm @ 0x718409a11e80]   Input stream #0:0 (video): 14 packets read (409241 bytes); 2 frames decoded; 0 decode errors;\n"
            + "Conversion failed!";

        [Theory]
        [InlineData(VideoToolboxOutput, "videotoolbox_vld")]
        [InlineData(SoftwareOutput, "yuv420p")]
        [InlineData("Stream mapping: Stream #0:0 -> #0:0 (h264 (native) -> h264 (libx264))", null)]
        public void DecodedPixelFormat_ReadsTheFilterGraphInput(string output, string? expected)
            => Assert.Equal(expected, TranscodeTestRules.DecodedPixelFormat(output));

        [Theory]
        [InlineData("-init_hw_device videotoolbox=vt -hwaccel videotoolbox -hwaccel_output_format videotoolbox_vld -i file:\"a.mkv\"", true)]
        [InlineData("-init_hw_device qsv=qs -c:v h264_qsv -i file:\"a.mkv\"", true)]
        [InlineData("-init_hw_device videotoolbox=vt -i file:\"a.mkv\"", false)]
        public void UsesHardwareDecoder_FindsHwaccelAndHardwareDecoders(string arguments, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.UsesHardwareDecoder(arguments));

        [Theory]
        [InlineData("-vf \"scale_vt=w=1920:h=1080:format=nv12:color_matrix=bt709:color_primaries=bt709:color_transfer=bt709\"", TranscodeTestTonemapMode.Hardware)]
        [InlineData("-vf \"setparams=color_primaries=bt2020,tonemapx=tonemap=bt2390:desat=0:peak=100:t=bt709:m=bt709:p=bt709:format=yuv420p\"", TranscodeTestTonemapMode.Software)]
        [InlineData("-vf \"hwupload=derive_device=opencl,tonemap_opencl=format=nv12:p=bt709:t=bt709:m=bt709\"", TranscodeTestTonemapMode.Hardware)]
        [InlineData("-vf \"scale_vt=w=1920:h=1080:format=nv12\"", TranscodeTestTonemapMode.None)]
        public void FindTonemapFilter_RecognisesEachMethod(string filters, TranscodeTestTonemapMode expected)
            => Assert.Equal(expected, TranscodeTestRules.TonemapMode(TranscodeTestRules.FindTonemapFilter(filters)));

        [Theory]
        [InlineData(true, 0, "videotoolbox_vld", true, TranscodeTestOutcome.Passed, TranscodeTestAdvice.None)]
        [InlineData(true, 0, "cuda", false, TranscodeTestOutcome.Passed, TranscodeTestAdvice.TurnOn)]
        [InlineData(true, 0, "yuv420p", true, TranscodeTestOutcome.Software, TranscodeTestAdvice.TurnOff)]
        [InlineData(true, 0, "yuv420p", false, TranscodeTestOutcome.Software, TranscodeTestAdvice.None)]
        [InlineData(true, 1, null, true, TranscodeTestOutcome.Failed, TranscodeTestAdvice.None)]
        [InlineData(false, null, null, true, TranscodeTestOutcome.NotSupported, TranscodeTestAdvice.TurnOff)]
        [InlineData(false, null, null, false, TranscodeTestOutcome.NotSupported, TranscodeTestAdvice.None)]
        public void Decode_AdvisesToMatchWhatTheHardwareDoes(bool hasDecoder, int? exitCode, string? pixelFormat, bool enabled, TranscodeTestOutcome outcome, TranscodeTestAdvice advice)
            => Assert.Equal((outcome, advice), TranscodeTestRules.Decode(hasDecoder, exitCode, pixelFormat, enabled));

        [Theory]
        [InlineData("h264", true, true, true)]
        [InlineData("h264", true, false, true)]
        [InlineData("h264", false, true, false)]
        [InlineData("h264", false, false, false)]
        [InlineData("hevc", true, true, true)]
        [InlineData("hevc", true, false, false)]
        [InlineData("hevc", false, true, false)]
        [InlineData("hevc", false, false, false)]
        [InlineData("av1", true, true, true)]
        [InlineData("av1", true, false, false)]
        [InlineData("av1", false, true, false)]
        [InlineData("av1", false, false, false)]
        public void IsEncodeEnabled_NeedsTheSwitchAndTheAllowOptionForHevcAndAv1(string codec, bool hardwareEncoding, bool allow, bool expected)
        {
            // The allow option is the one for the codec under test; the other one stays the opposite, to catch mix-ups.
            var saved = new EncodingOptions
            {
                EnableHardwareEncoding = hardwareEncoding,
                AllowHevcEncoding = codec == "hevc" ? allow : !allow,
                AllowAv1Encoding = codec == "av1" ? allow : !allow
            };

            Assert.Equal(expected, TranscodeTestRules.IsEncodeEnabled(codec, saved));
        }

        [Theory]
        [InlineData("h264_videotoolbox", 0, true, true, TranscodeTestOutcome.Passed, TranscodeTestAdvice.None)]
        [InlineData("hevc_nvenc", 0, false, true, TranscodeTestOutcome.Passed, TranscodeTestAdvice.TurnOn)]
        [InlineData("hevc_nvenc", 0, true, false, TranscodeTestOutcome.Passed, TranscodeTestAdvice.None)]
        [InlineData("av1_qsv", 0, false, false, TranscodeTestOutcome.Passed, TranscodeTestAdvice.None)]
        [InlineData("hevc_qsv", 1, true, true, TranscodeTestOutcome.Failed, TranscodeTestAdvice.None)]
        [InlineData("av1_qsv", 1, true, false, TranscodeTestOutcome.Failed, TranscodeTestAdvice.None)]
        [InlineData("h264_qsv", 1, false, true, TranscodeTestOutcome.Failed, TranscodeTestAdvice.None)]
        [InlineData("libsvtav1", null, true, true, TranscodeTestOutcome.NotSupported, TranscodeTestAdvice.None)]
        public void Encode_AdvisesToMatchWhatTheHardwareDoes(string encoder, int? exitCode, bool hardwareEncoding, bool allowed, TranscodeTestOutcome outcome, TranscodeTestAdvice advice)
            => Assert.Equal((outcome, advice), TranscodeTestRules.Encode(encoder, exitCode, hardwareEncoding, allowed));

        [Fact]
        public void FramesDecoded_ReadsFfmpegsFinalStatistics()
        {
            const string Output = "[in#0/matroska,webm @ 0x0] Input file #0 (h264.mkv):\n"
                + "[in#0/matroska,webm @ 0x0]   Input stream #0:0 (video): 31 packets read (52011 bytes); 30 frames decoded; 0 decode errors;\n"
                + "[in#0/matroska,webm @ 0x0]   Total: 31 packets (52011 bytes) demuxed";

            Assert.Equal(30, TranscodeTestRules.FramesDecoded(Output));
            Assert.Equal(0, TranscodeTestRules.FramesDecoded("Input stream #0:0 (video): 0 packets read (0 bytes); 0 frames decoded; 0 decode errors;"));
            Assert.Null(TranscodeTestRules.FramesDecoded("[h264 @ 0x0] Error while decoding stream #0:0"));
        }

        [Theory]
        [InlineData(30, "[h264_nvenc @ 0x0] Error", true)]
        [InlineData(0, "[vost#0:0/h264_nvenc @ 0x0] Error while opening encoder", false)] // The count decides when ffmpeg gave it.
        [InlineData(null, "[vost#0:0/h264_nvenc @ 0x0] [enc:h264_nvenc @ 0x1] Error while opening encoder", true)]
        [InlineData(null, "[h264_cuvid @ 0x0] cuvidCreateDecoder failed", false)]
        [InlineData(null, null, false)]
        public void FailedAfterDecoding_NeedsDecodedFramesOrAnOutputStreamError(int? frames, string? errors, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.FailedAfterDecoding(frames, errors));

        [Fact]
        public void FramesEncoded_ReadsFfmpegsFinalStatistics()
        {
            const string Output = "[out#0/null @ 0x0] Output file #0 (pipe:):\n"
                + "[out#0/null @ 0x0]   Output stream #0:0 (video): 96 frames encoded; 96 packets muxed (123 bytes);\n"
                + "[out#0/null @ 0x0]   Total: 96 packets (123 bytes) muxed";

            Assert.Equal(96, TranscodeTestRules.FramesEncoded(Output));
            Assert.Equal(0, TranscodeTestRules.FramesEncoded("Output stream #0:0 (video): 0 frames encoded; 0 packets muxed (0 bytes);"));
            Assert.Null(TranscodeTestRules.FramesEncoded("[av1_nvenc @ 0x0] Error while opening encoder"));
        }

        [Theory]
        [InlineData(true, true, 0, TranscodeTestOutcome.NotSupported)] // Explicit capability evidence is also supplied.
        [InlineData(false, true, 0, TranscodeTestOutcome.Failed)] // H.264 failed or was not tested: the setup may be broken.
        [InlineData(true, true, null, TranscodeTestOutcome.Failed)] // ffmpeg did not say how many frames.
        [InlineData(true, true, 5, TranscodeTestOutcome.Failed)] // Frames got through, so something else failed.
        [InlineData(true, false, 0, TranscodeTestOutcome.Failed)] // Decoding failed with the GPU encoder, which is not ruled out.
        public void FailedOutcome_ExplicitFormatFailureStillNeedsNoFramesAndAPassedReference(bool referencePassed, bool otherHalfRuledOut, int? frames, TranscodeTestOutcome expected)
            => Assert.Equal(expected, TranscodeTestRules.FailedOutcome(referencePassed, otherHalfRuledOut, frames, output: "[av1_nvenc @ 0x0] Codec not supported"));

        [Theory]
        [InlineData(0, true, TranscodeTestOutcome.NotSupported, false)]
        [InlineData(0, false, TranscodeTestOutcome.Failed, false)]
        [InlineData(null, true, TranscodeTestOutcome.Failed, false)]
        [InlineData(12, true, TranscodeTestOutcome.Failed, true)] // Still reported as failing after decoding.
        public void FailedDecode_AfterTheCpuEncoderRetry_IsConcludedFromTheFramesAndTheReference(int? frames, bool referencePassed, TranscodeTestOutcome outcome, bool failedAfterDecoding)
        {
            var (failed, advice) = TranscodeTestRules.Decode(true, 1, null, true);

            Assert.Equal(TranscodeTestOutcome.Failed, failed);
            Assert.Equal(outcome, TranscodeTestRules.FailedOutcome(referencePassed, true, frames, output: "[hevc_cuvid @ 0x0] unsupported bit depth: 12"));
            Assert.Equal(failedAfterDecoding, TranscodeTestRules.FailedAfterDecoding(frames, null));
            Assert.Equal(TranscodeTestAdvice.None, advice); // A generic failure alone does not justify changing settings.
        }

        [Theory]
        [InlineData("[av1_nvenc @ 0x0] Codec not supported\nOutput stream #0:0 (video): 0 frames encoded; 0 packets muxed (0 bytes);", true, TranscodeTestOutcome.NotSupported)]
        [InlineData("Output stream #0:0 (video): 0 frames encoded; 0 packets muxed (0 bytes);", true, TranscodeTestOutcome.Failed)]
        [InlineData("Output stream #0:0 (video): 0 frames encoded; 0 packets muxed (0 bytes);", false, TranscodeTestOutcome.Failed)]
        [InlineData("[av1_nvenc @ 0x0] Error while opening encoder", true, TranscodeTestOutcome.Failed)]
        [InlineData("Output stream #0:0 (video): 40 frames encoded; 40 packets muxed (9000 bytes);", true, TranscodeTestOutcome.Failed)]
        public void FailedEncode_IsConcludedFromTheFramesEncodedAndTheReference(string output, bool referencePassed, TranscodeTestOutcome outcome)
        {
            var (failed, advice) = TranscodeTestRules.Encode("av1_nvenc", 1, true, true);

            Assert.Equal(TranscodeTestOutcome.Failed, failed);
            Assert.Equal(outcome, TranscodeTestRules.FailedOutcome(referencePassed, true, TranscodeTestRules.FramesEncoded(output), output: output));
            Assert.Equal(TranscodeTestAdvice.None, advice);
        }

        [Theory]
        [InlineData("[av1_nvenc @ 0x0] Codec not supported\n" + Rtx3070Av1Output, true, TranscodeTestOutcome.NotSupported)]
        [InlineData(Rtx3070Av1Output, true, TranscodeTestOutcome.Failed)]
        [InlineData(Rtx3070Av1Output, false, TranscodeTestOutcome.Failed)] // H.264 encoding did not pass.
        [InlineData("frame=    0 fps=0.0 q=0.0 size=       0KiB\r" + Rtx3070Av1Output, true, TranscodeTestOutcome.Failed)]
        [InlineData("[av1_nvenc @ 0x0] Error while opening encoder", true, TranscodeTestOutcome.Failed)]
        [InlineData("[vost#0:0/av1_nvenc @ 0x0] [enc:av1_nvenc @ 0x1] Could not open encoder before EOF", true, TranscodeTestOutcome.Failed)]
        [InlineData("Output stream #0:0 (video): 0 frames encoded; 0 packets muxed (0 bytes);", true, TranscodeTestOutcome.Failed)]
        [InlineData("Output stream #0:0 (video): 12 frames encoded; 12 packets muxed (9000 bytes);\n" + Rtx3070Av1Output, true, TranscodeTestOutcome.Failed)]
        [InlineData("frame=   12 fps=0.0 q=0.0 size=       0KiB\r" + Rtx3070Av1Output, true, TranscodeTestOutcome.Failed)] // Frames got out first.
        [InlineData("[vost#0:1/aac @ 0x0] Task finished with error code: -22 (Invalid argument)\nConversion failed!", true, TranscodeTestOutcome.Failed)]
        [InlineData("[vost#0:0/h264_nvenc @ 0x0] Error while opening encoder\nConversion failed!", true, TranscodeTestOutcome.Failed)]
        [InlineData("[hevc @ 0x0] Error while decoding stream #0:0\nConversion failed!", true, TranscodeTestOutcome.Failed)]
        public void FailedEncode_WithoutAFrameCount_NeedsExplicitFormatEvidenceAndNoOutput(string output, bool referencePassed, TranscodeTestOutcome outcome)
        {
            var (failed, advice) = TranscodeTestRules.Encode("av1_nvenc", 1, true, true);

            Assert.Equal(TranscodeTestOutcome.Failed, failed);
            Assert.Equal(outcome, TranscodeTestRules.FailedOutcome(referencePassed, true, TranscodeTestRules.EncodedFrames(output, "av1_nvenc"), output: output));
            Assert.Equal(TranscodeTestAdvice.None, advice);
        }

        [Fact]
        public void IsStressTestItem_AcceptsAnyVideo()
        {
            Assert.True(TranscodeTestRules.IsStressTestItem(new Movie()));
            Assert.True(TranscodeTestRules.IsStressTestItem(new Episode()));
            Assert.True(TranscodeTestRules.IsStressTestItem(new MusicVideo()));
            Assert.True(TranscodeTestRules.IsStressTestItem(new Video()));
            Assert.False(TranscodeTestRules.IsStressTestItem(new Audio()));
            Assert.False(TranscodeTestRules.IsStressTestItem(new Folder()));
            Assert.False(TranscodeTestRules.IsStressTestItem(null));
        }

        [Theory]
        [InlineData(30.0, "This file is 30 s long. Each stream restarts at its end, which briefly disturbs the measurement; a file of at least a minute gives steadier results.")]
        [InlineData(59.5, "This file is 59.5 s long. Each stream restarts at its end, which briefly disturbs the measurement; a file of at least a minute gives steadier results.")]
        [InlineData(60.0, null)]
        [InlineData(5400.0, null)]
        [InlineData(0.0, null)] // A run time of 0 is not known.
        [InlineData(null, null)]
        public void DescribeShortSource_WarnsForAFileUnderAMinute(double? seconds, string? expected)
        {
            Assert.Equal(expected, TranscodeTestRules.DescribeShortSource(seconds));
            Assert.Equal(expected is not null, TranscodeTestRules.IsShortSource(seconds));
        }

        [Fact]
        public void ErrorLines_SkipsEncoderLibraryWarnings()
        {
            // What libsvtav1 printed in a container on an Intel N5095: only a harmless warning.
            const string Output = "Svt[info]: SVT [version]: SVT-AV1 Encoder Lib v3.0.0\nSvt[warn]: Failed to set thread priority: Invalid argument\n";

            Assert.Equal(string.Empty, TranscodeTestRules.ErrorLines(Output));
        }

        [Theory]
        // A clean exit after too few frames says so first.
        [InlineData(0, 12, 96, "", "The check produced only 12 of 96 required frames.")]
        [InlineData(0, 12, 96, "x", "The check produced only 12 of 96 required frames. x")]
        // A failed run keeps its own diagnostic at the front: VP8 failed after decoding 2 frames.
        [InlineData(1, 0, 96, "[vp8] Failed to decode", "[vp8] Failed to decode")]
        [InlineData(-1, 0, 96, "timed out after 60 s", "timed out after 60 s")]
        [InlineData(0, 96, 96, "", "")]
        [InlineData(0, null, 96, "", "")]
        [InlineData(0, 12, null, "", "")]
        public void CheckErrors_CountsMissingFramesOnlyForACleanExit(int exitCode, int? encoded, int? required, string errors, string expected)
            => Assert.Equal(expected, TranscodeTestRules.CheckErrors(exitCode, encoded, required, errors));

        [Fact]
        public void ErrorLines_KeepsTheLastThreeErrors()
        {
            const string Output = "[a] Error 1\nfine\n[b] Error 2\nSvt[warn]: Failed to set thread priority\n[c] Task finished with error code: -22\nConversion failed!\n";

            Assert.Equal("[b] Error 2 | [c] Task finished with error code: -22 | Conversion failed!", TranscodeTestRules.ErrorLines(Output));
        }

        [Theory]
        [InlineData(1, "exit code 1")]
        [InlineData(-1, "exit code -1")]
        [InlineData(137, "exit code 137, ended by signal 9, killed (often because it ran out of memory)")]
        [InlineData(132, "exit code 132, ended by signal 4, illegal instruction (the CPU may lack an instruction this encoder needs)")]
        [InlineData(139, "exit code 139, ended by signal 11")]
        [InlineData(234, "exit code 234")]
        public void DescribeExit_NamesTheSignal(int exitCode, string expected)
            => Assert.Equal(expected, TranscodeTestRules.DescribeExit(exitCode));

        [Theory]
        [InlineData(HardwareAccelerationType.vaapi, true, false, true)]
        [InlineData(HardwareAccelerationType.vaapi, true, true, false)]
        [InlineData(HardwareAccelerationType.vaapi, false, false, false)]
        [InlineData(HardwareAccelerationType.qsv, true, false, false)]
        public void NeedsVaapiRestart_OnlyWhenTheVaapiDeviceIsUnidentifiedOnLinux(HardwareAccelerationType type, bool isLinux, bool identified, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.NeedsVaapiRestart(type, isLinux, identified));

        [Fact]
        public void ClipArguments_MakesA1080pClip()
            => Assert.Equal(
                "-hide_banner -v error -y -f lavfi -i testsrc2=size=1920x1080:rate=24 -t 4 -c:v libsvtav1 -pix_fmt yuv420p10le -preset 10 \"/cache/av1.mkv\"",
                TranscodeTestRules.ClipArguments("libsvtav1", "yuv420p10le", "-preset 10", false, "/cache/av1.mkv", false));

        [Fact]
        public void ClipArguments_SmallerClipIs640x360WithTheSameOptions()
            => Assert.Equal(
                "-hide_banner -v error -y -f lavfi -i testsrc2=size=640x360:rate=24 -t 4 -c:v libsvtav1 -pix_fmt yuv420p10le -preset 10 \"/cache/av1.mkv\"",
                TranscodeTestRules.ClipArguments("libsvtav1", "yuv420p10le", "-preset 10", false, "/cache/av1.mkv", true));

        [Fact]
        public void ClipArguments_KeepsTheHdrTag()
            => Assert.Equal(
                "-hide_banner -v error -y -f lavfi -i testsrc2=size=640x360:rate=24 -t 4 -vf setparams=color_primaries=bt2020:color_trc=smpte2084:colorspace=bt2020nc -c:v libx265 -pix_fmt yuv420p10le -x265-params log-level=none:hdr10=1 \"/cache/hevc10-hdr10.mkv\"",
                TranscodeTestRules.ClipArguments("libx265", "yuv420p10le", "-x265-params log-level=none:hdr10=1", true, "/cache/hevc10-hdr10.mkv", true));

        [Theory]
        [InlineData("tonemap_opencl=format=nv12", 0, false, TranscodeTestOutcome.Passed, TranscodeTestAdvice.TurnOn)]
        [InlineData("tonemapx=tonemap=bt2390", 0, true, TranscodeTestOutcome.Software, TranscodeTestAdvice.None)]
        [InlineData("tonemap_cuda=format=yuv420p", 1, true, TranscodeTestOutcome.Failed, TranscodeTestAdvice.TurnOff)]
        [InlineData(null, null, false, TranscodeTestOutcome.NotSupported, TranscodeTestAdvice.None)]
        public void Tonemap_ReportsWhereItRuns(string? filter, int? exitCode, bool enabled, TranscodeTestOutcome outcome, TranscodeTestAdvice advice)
            => Assert.Equal((outcome, advice), TranscodeTestRules.Tonemap(filter, exitCode, enabled));

        [Theory]
        [InlineData(23.96, 23.976, "23.96")] // One decimal would read 24.0, above the threshold.
        [InlineData(18.44, 23.976, "18.4")]
        [InlineData(23.9759, 23.976, "23.975")] // Rounding to three decimals would land on the threshold.
        [InlineData(24.03, 24.0, "24.03")] // Above the threshold stays above it.
        [InlineData(23.98, 24.0, "23.98")]
        [InlineData(24.0, 24.0, "24.0")]
        [InlineData(123.6, 23.976, "123.6")]
        public void RoundFps_KeepsAReadingOnItsSideOfTheThreshold(double fps, double threshold, string expected)
        {
            var rounded = TranscodeTestRules.RoundFps(fps, threshold);
            Assert.Equal(expected, TranscodeTestRules.FormatFps(rounded));
            Assert.Equal(Math.Sign(fps.CompareTo(threshold)), Math.Sign(rounded.CompareTo(threshold)));
        }

        [Theory]
        [InlineData(23.976, "23.976")]
        [InlineData(24.0, "24.0")]
        [InlineData(29.97, "29.97")]
        public void FormatFps_ShowsTheSourcesPrecision(double fps, string expected)
            => Assert.Equal(expected, TranscodeTestRules.FormatFps(fps));

        [Fact]
        public void DescribeResult_SaysHowManyStreamsWereRunningWhenOneFellBehind()
        {
            var result = new TranscodeTestStressResult { EndReason = TranscodeTestEndReason.LimitReached, MaxStableStreams = 4, StreamsAtEnd = 5, FailedStreamId = 5, FailedStreamFps = 20.04 };
            Assert.Equal("Result: 4 stream(s) kept up with real time. With 5 streams running, stream 5 fell to 20.04 fps.", TranscodeTestRules.DescribeResult(result));

            result.EndReason = TranscodeTestEndReason.MaxStreams;
            Assert.Equal("Result: 4 stream(s) kept up with real time.", TranscodeTestRules.DescribeResult(result));
        }

        [Fact]
        public void DescribeDip_NamesTheNextStream()
        {
            Assert.Equal("Stream 3 dipped to 23.1 fps; stream 8 waits until every stream has kept up for 3 s.", TranscodeTestRules.DescribeDip(3, 23.14, 23.976, 3, 8));
            Assert.Equal("Stream 1 dipped to 0.0 fps; the test ends once every stream has kept up for 3 s.", TranscodeTestRules.DescribeDip(1, 0, 24, 3, null));
        }

        [Fact]
        public void DescribeRamp_SaysAStreamIsAddedAtMostEveryInterval()
            => Assert.Equal(
                "A stream is added at most every 12 s, once every running stream has kept up for 3 s.",
                TranscodeTestRules.DescribeRamp(TranscodeTestManager.RampSeconds, TranscodeTestStream.WindowSeconds));

        [Fact]
        public void LogText_UsesADecimalPointInEveryCulture()
        {
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                double single = 61.25;
                double speed = 2.554;
                Assert.Equal("One stream alone ran at 61.3 fps (2.55× real time).", TranscodeTestManager.FormatLog($"One stream alone ran at {single:0.0} fps ({speed:0.00}× real time)."));
                Assert.Equal("Stream 3 dipped to 23.1 fps; stream 8 waits until every stream has kept up for 3 s.", TranscodeTestRules.DescribeDip(3, 23.14, 23.976, 3, 8));
                var result = new TranscodeTestStressResult { EndReason = TranscodeTestEndReason.LimitReached, MaxStableStreams = 4, StreamsAtEnd = 5, FailedStreamId = 5, FailedStreamFps = 20.5 };
                Assert.EndsWith("stream 5 fell to 20.5 fps.", TranscodeTestRules.DescribeResult(result), StringComparison.Ordinal);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }
    }
}
