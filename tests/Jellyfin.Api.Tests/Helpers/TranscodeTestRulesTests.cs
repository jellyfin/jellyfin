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
        private const string Session = "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: incompatible client key (21): (no details)";
        private const string Gpu = "[av1 @ 0x0] cuvidCreateDecoder(&ctx->cudecoder, &cuinfo) failed -> CUDA_ERROR_OUT_OF_MEMORY: out of memory";
        private const string SoftwareOutput = "[graph -1 input from stream 0:0 @ 0x9a50486c0] w:1280 h:720 pixfmt:yuv420p tb:1/1000 fr:24/1 sar:1/1 csp:bt470bg";

        // This excerpt from an RTX 3070 omits the capability diagnostic. Its generic error alone proves no cause.
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

        [Theory]
        [InlineData(null, false)]
        [InlineData("copy", false)]
        [InlineData("libx264", false)]
        [InlineData("libx265", false)]
        [InlineData("libsvtav1", false)]
        [InlineData("mpeg4", false)]
        [InlineData("unknown_nvenc", false)]
        [InlineData("h264_amf", true)]
        [InlineData("h264_nvenc", true)]
        [InlineData("hevc_qsv", true)]
        [InlineData("hevc_vaapi", true)]
        [InlineData("av1_videotoolbox", true)]
        [InlineData("h264_v4l2m2m", true)]
        [InlineData("av1_rkmpp", true)]
        [InlineData("HEVC_NVENC", true)]
        public void IsHardwareEncoder_NeedsAnActualHardwareCodec_NotCopy(string? encoder, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.IsHardwareEncoder(encoder));

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
        [InlineData("h264_nvenc", "[h264_nvenc @ 0x5581] OpenEncodeSessionEx failed: out of memory (10): (no details) | Conversion failed!", true)]
        [InlineData("hevc_nvenc", "[hevc_nvenc @ 0x5581] OpenEncodeSessionEx failed: incompatible client key (21): (no details)", true)]
        [InlineData("hevc_nvenc", "[hevc_nvenc @ 0x5581] Task finished with error code: -22 (Invalid argument)", false)]
        [InlineData("h264_qsv", "Error: out of memory (10)", false)]
        [InlineData("h264_nvenc", null, false)]
        [InlineData(null, "OpenEncodeSessionEx failed: out of memory (10)", false)]
        public void IsNvencSessionLimit_NeedsAnNvencEncoderAndTheDriversRefusal(string? encoder, string? errors, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.IsNvencSessionLimit(encoder, errors));

        [Theory]
        // NVIDIA's resource/license refusal stays neutral, whatever came before.
        [InlineData("h264_nvenc", "OpenEncodeSessionEx failed: out of memory (10): (no details)", 0, true, TranscodeTestEndReason.NvencSessionRefused, false, 3)]
        [InlineData("hevc_nvenc", "OpenEncodeSessionEx failed: incompatible client key (21)", 2, true, TranscodeTestEndReason.NvencSessionRefused, false, 2)]
        [InlineData("av1_nvenc", "OpenEncodeSessionEx failed: out of memory (10)", 8, false, TranscodeTestEndReason.NvencSessionRefused, false, 8)]
        // A historical count is no more conclusive than any other count.
        [InlineData("h264_nvenc", "OpenEncodeSessionEx failed: out of memory (10)", 6, true, TranscodeTestEndReason.NvencSessionRefused, false, 7)]
        // The RTX 5080 report: K streams kept up, the next never started, ffmpeg said nothing useful. A hang is never
        // blamed on NVIDIA's session limit.
        [InlineData("h264_nvenc", "", 8, true, TranscodeTestEndReason.StreamStalled, true)]
        [InlineData("h264_nvenc", "", 9, true, TranscodeTestEndReason.StreamStalled, true)]
        [InlineData("h264_nvenc", "", 3, true, TranscodeTestEndReason.StreamStalled, true)]
        [InlineData("h264_nvenc", "", 2, true, TranscodeTestEndReason.StreamStalled, true)]
        // Other encoders: the same capacity wording, never NVIDIA's limit.
        [InlineData("h264_videotoolbox", "OpenEncodeSessionEx failed: out of memory (10)", 8, true, TranscodeTestEndReason.StreamStalled, true)]
        [InlineData("libx264", "", 8, true, TranscodeTestEndReason.StreamStalled, true)]
        [InlineData(null, "", 4, true, TranscodeTestEndReason.StreamStalled, true)]
        // A real failure: nothing kept up, or an older stream stalled.
        [InlineData("h264_nvenc", "", 0, true, TranscodeTestEndReason.StreamStalled, false)]
        [InlineData("h264_nvenc", "", 8, false, TranscodeTestEndReason.StreamStalled, false)]
        public void StallOutcome_ReportsCapacityAndTheDriverLimitHonestly(string? encoder, string evidence, int maxStable, bool isNewest, TranscodeTestEndReason reason, bool couldNotStart, int otherSessions = 12)
            => Assert.Equal((reason, couldNotStart), TranscodeTestRules.StallOutcome(encoder, evidence, maxStable, isNewest, otherSessions));

        [Theory]
        [InlineData(TranscodeTestEndReason.LimitReached, TranscodeTestSurveyFailureReason.Performance)]
        [InlineData(TranscodeTestEndReason.StreamStalled, TranscodeTestSurveyFailureReason.FailedTimeout)]
        [InlineData(TranscodeTestEndReason.NvencSessionRefused, TranscodeTestSurveyFailureReason.FailedNvencLimit)]
        [InlineData(TranscodeTestEndReason.MaxStreams, null)]
        [InlineData(TranscodeTestEndReason.StreamFailed, null)]
        [InlineData(TranscodeTestEndReason.Cancelled, null)]
        [InlineData(TranscodeTestEndReason.None, null)]
        public void SurveyFailureReason_UsesTheHardwareSurveysNames(TranscodeTestEndReason reason, TranscodeTestSurveyFailureReason? expected)
            => Assert.Equal(expected, TranscodeTestRules.SurveyFailureReason(reason));

        [Theory]
        [InlineData("tonemap_opencl=format=nv12", 0, false, TranscodeTestOutcome.Passed, TranscodeTestAdvice.TurnOn)]
        [InlineData("tonemapx=tonemap=bt2390", 0, true, TranscodeTestOutcome.Software, TranscodeTestAdvice.None)]
        [InlineData("tonemap_cuda=format=yuv420p", 1, true, TranscodeTestOutcome.Failed, TranscodeTestAdvice.TurnOff)]
        [InlineData(null, null, false, TranscodeTestOutcome.NotSupported, TranscodeTestAdvice.None)]
        public void Tonemap_ReportsWhereItRuns(string? filter, int? exitCode, bool enabled, TranscodeTestOutcome outcome, TranscodeTestAdvice advice)
            => Assert.Equal((outcome, advice), TranscodeTestRules.Tonemap(filter, exitCode, enabled));

        [Theory]
        [InlineData("[av1 @ 0x0] cuvidCreateDecoder(&ctx->cudecoder, &cuinfo) failed -> CUDA_ERROR_OUT_OF_MEMORY: out of memory", true)]
        [InlineData("[AVHWDeviceContext @ 0x0] cuMemAlloc(&data, size) failed -> CUDA_ERROR_OUT_OF_MEMORY: out of memory", true)]
        [InlineData("[hevc @ 0x0] Failed to create surface: 2 (resource allocation failed).", true)]
        [InlineData("[h264_qsv @ 0x0] Error initializing the decoder: failed to allocate memory (-4)", true)]
        [InlineData("[Parsed_libplacebo_0 @ 0x0] vkAllocateMemory failed: VK_ERROR_OUT_OF_DEVICE_MEMORY", true)]
        [InlineData("[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)", false)]
        [InlineData("[in#0 @ 0x0] Error opening input: Cannot allocate memory", false)]
        [InlineData("[Parsed_libplacebo_0 @ 0x0] vkCreateDevice failed: VK_ERROR_OUT_OF_HOST_MEMORY", false)]
        [InlineData("[h264_qsv @ 0x0] Error initializing the decoder: failed to allocate memory (-5)", false)]
        [InlineData("Conversion failed!", false)]
        [InlineData(null, false)]
        public void IsGpuMemoryLine_MatchesOnlyDeviceMemoryErrors(string? line, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.IsGpuMemoryLine(line));

        [Theory]
        [InlineData("OpenEncodeSessionEx failed: out of memory (10)", true)]
        [InlineData("OpenEncodeSessionEx failed: INCOMPATIBLE CLIENT KEY (21)", true)]
        [InlineData("Conversion failed!", false)]
        [InlineData(null, false)]
        public void IsSessionLimitText_IsTheRefusalWhateverTheEncoder(string? text, bool expected)
        {
            Assert.Equal(expected, TranscodeTestRules.IsSessionLimitText(text));
            Assert.Equal(expected, TranscodeTestRules.IsNvencSessionLimit("h264_nvenc", text));
            Assert.False(TranscodeTestRules.IsNvencSessionLimit("h264_videotoolbox", text));
        }

        [Theory]
        // The session refusal wins, for both kinds, whatever K, the stream or its frames.
        [InlineData(true, "h264_nvenc", Session, Gpu, 0, false, true, TranscodeTestEndReason.NvencSessionRefused, false)]
        [InlineData(false, "hevc_nvenc", Session, null, 12, true, false, TranscodeTestEndReason.NvencSessionRefused, false)]
        // Session text without NVENC is not the driver limit; a GPU line then decides.
        [InlineData(true, "h264_videotoolbox", Session, Gpu, 2, true, false, TranscodeTestEndReason.GpuMemoryFull, false)]
        [InlineData(true, "h264_videotoolbox", Session, null, 2, true, false, TranscodeTestEndReason.StreamFailed, true)]
        // GPU memory after K >= 1, for any stream; at K = 0 it stays a failure.
        [InlineData(true, "av1_nvenc", null, Gpu, 15, true, false, TranscodeTestEndReason.GpuMemoryFull, false)]
        [InlineData(false, "av1_nvenc", null, Gpu, 15, true, false, TranscodeTestEndReason.GpuMemoryFull, false)]
        [InlineData(true, "hevc_vaapi", null, Gpu, 1, false, true, TranscodeTestEndReason.GpuMemoryFull, false)]
        [InlineData(true, "av1_nvenc", null, Gpu, 0, true, false, TranscodeTestEndReason.StreamFailed, false)]
        [InlineData(false, "av1_nvenc", null, Gpu, 0, true, false, TranscodeTestEndReason.StreamStalled, false)]
        // The newest stream exiting before its first frame is a capacity result, with no NVIDIA hint.
        [InlineData(true, "h264_nvenc", null, null, 22, true, false, TranscodeTestEndReason.StreamFailed, true)]
        [InlineData(true, "h264_nvenc", null, null, 3, true, false, TranscodeTestEndReason.StreamFailed, true)]
        [InlineData(true, "h264_nvenc", null, null, 2, true, false, TranscodeTestEndReason.StreamFailed, true)]
        [InlineData(true, "h264_videotoolbox", null, null, 1, true, false, TranscodeTestEndReason.StreamFailed, true)]
        // After frames, an older stream, or K = 0, an exit stays a red failure.
        [InlineData(true, "h264_nvenc", null, null, 22, true, true, TranscodeTestEndReason.StreamFailed, false)]
        [InlineData(true, "h264_nvenc", null, null, 22, false, false, TranscodeTestEndReason.StreamFailed, false)]
        [InlineData(true, "h264_nvenc", null, null, 0, true, false, TranscodeTestEndReason.StreamFailed, false)]
        // A stall of the newest stream after K >= 1 is a capacity result only before its first frame.
        [InlineData(false, "h264_nvenc", null, null, 3, true, true, TranscodeTestEndReason.StreamStalled, false)]
        [InlineData(false, "h264_nvenc", null, null, 3, true, false, TranscodeTestEndReason.StreamStalled, true)]
        [InlineData(false, "h264_nvenc", null, null, 3, false, false, TranscodeTestEndReason.StreamStalled, false)]
        [InlineData(false, "h264_videotoolbox", null, null, 15, true, false, TranscodeTestEndReason.StreamStalled, true)]
        public void EndOutcome_NamesTheCauseForExitsAndStalls(bool exited, string? encoder, string? session, string? gpu, int maxStable, bool isNewest, bool frames, TranscodeTestEndReason reason, bool couldNotStart)
        {
            var kind = exited ? TranscodeTestEndKind.Exited : TranscodeTestEndKind.Stalled;
            Assert.Equal((reason, couldNotStart), TranscodeTestRules.EndOutcome(kind, encoder, session, gpu, maxStable, isNewest, frames, 12));
        }

        [Fact]
        public void DescribeCouldNotStart_SaysAStalledStreamStartedButProducedNoFrames()
        {
            Assert.Equal(
                "3 stream(s) kept up; stream 4 started but produced no frames in 30 s.",
                TranscodeTestRules.DescribeCouldNotStart(TranscodeTestEndKind.Stalled, 3, 4, 30));
            Assert.Equal(
                "3 stream(s) kept up; the server could not start stream 4.",
                TranscodeTestRules.DescribeCouldNotStart(TranscodeTestEndKind.Exited, 3, 4, 30));
        }

        [Theory]
        [InlineData(false, 1.0, true)]
        [InlineData(true, 1.0, false)]
        [InlineData(false, 12.5, false)]
        [InlineData(false, null, false)]
        public void IsWaitingOnInput_OnlyBeforeTheFileIsOpenedWithLittleCpuTime(bool sawInput, double? cpuSeconds, bool expected)
        {
            TimeSpan? cpu = cpuSeconds is double c ? TimeSpan.FromSeconds(c) : null;
            Assert.Equal(expected, TranscodeTestRules.IsWaitingOnInput(TranscodeTestEndKind.Stalled, sawInput, cpu));
        }

        [Fact]
        public void IsWaitingOnInput_NotForAStreamThatExited()
            => Assert.False(TranscodeTestRules.IsWaitingOnInput(TranscodeTestEndKind.Exited, false, TimeSpan.FromSeconds(1)));

        [Fact]
        public void DescribeWaitingOnInput_PointsAtTheStorage()
        {
            Assert.Equal(
                "4 stream(s) kept up. Stream 5 did not start within 30 s: ffmpeg had not opened the file yet and used 1.0 s of CPU time, so it was waiting, probably on reading the media. Slow or shared storage, not the GPU, may be the limit.",
                TranscodeTestRules.DescribeWaitingOnInput(4, 5, 30, TimeSpan.FromSeconds(1)));
            Assert.StartsWith(
                "Stream 1 did not start within 30 s: ffmpeg had not opened the file yet and used 0.6 s",
                TranscodeTestRules.DescribeWaitingOnInput(null, 1, 30, TimeSpan.FromSeconds(0.6)),
                StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("cause", new[] { "a", "b" }, "cause | a | b")]
        [InlineData("cause", new[] { "x", "cause", "y" }, "x | cause | y")]
        [InlineData("cause", new string[0], "cause")]
        [InlineData("cause", new[] { "a cause b" }, "cause | a cause b")] // Whole lines are compared, not text inside them.
        [InlineData(null, new[] { "a" }, "a")]
        [InlineData("", new[] { "a" }, "a")]
        public void WithCause_PutsTheCauseFirstWhenItIsMissing(string? cause, string[] kept, string expected)
            => Assert.Equal(expected, TranscodeTestRules.WithCause(cause, kept));

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
        public void IsCollapse_Pes502Rtx3070_EveryStreamNearZeroAfterKeepingUp()
        {
            // Eight streams at about 61 fps (498.9 combined), then near 0 after stream 9 was added.
            double?[] fps = [0, 0.4, 1.7, 0, 0.9, 1.2, 0, 0.3, 0.6];
            Assert.True(TranscodeTestRules.IsCollapse(fps, 60));
        }

        [Fact]
        public void IsCollapse_GradualLimit_IsNot()
        {
            double?[] fps = [22.7, 24.1, 24.3, 24.0, 24.6, 24.2, 24.4, 24.1];
            Assert.False(TranscodeTestRules.IsCollapse(fps, 23.976));
        }

        [Fact]
        public void IsCollapse_OneStreamStalledWhileTheOthersKeepUp_IsNot()
        {
            double?[] fps = [0, 61.2, 61.5, 61.0, 62.0, 61.8, 61.3, 61.4];
            Assert.False(TranscodeTestRules.IsCollapse(fps, 60));
        }

        [Fact]
        public void IsCollapse_ModerateSlowdownIsNotCollapseRegardlessOfAHistoricalPeak()
        {
            // No stream is under a quarter of the threshold, but together they fell from 498.9 to 117 fps.
            double?[] fps = [16, 15.5, 16.2, 15.8, 16, 15.9, 21.6];
            Assert.False(TranscodeTestRules.IsCollapse(fps, 60));

            // Not when streams that are not measured yet would make the sum look low.
            double?[] unmeasured = [16, 15.5, 16.2, 15.8, 16, null, null];
            Assert.False(TranscodeTestRules.IsCollapse(unmeasured, 60));
        }

        [Fact]
        public void IsCollapse_AuditCounterexample_TenMildlySlowStreamsAreNotCollapse()
        {
            // The audit used an unrelated aggregate peak of 1000 fps: that must not certify a collapse at this load.
            double?[] fps = [23, 23, 23, 23, 23, 23, 23, 23, 23, 23];
            Assert.False(TranscodeTestRules.IsCollapse(fps, 24));
        }

        [Theory]
        [InlineData(0.0, false)]
        [InlineData(-1.0, false)]
        [InlineData(double.NaN, false)]
        [InlineData(double.PositiveInfinity, false)]
        [InlineData(24.0, true)]
        public void IsCollapse_RequiresAValidTarget(double threshold, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.IsCollapse([0, 0, 0], threshold));

        [Fact]
        public void IsCollapse_AllButOneStreamMustHaveEvidenceOfSevereSlowdown()
        {
            Assert.True(TranscodeTestRules.IsCollapse([0, 1, 2, null], 24));
            Assert.True(TranscodeTestRules.IsCollapse([0, 1, 2, 24], 24));
            Assert.False(TranscodeTestRules.IsCollapse([0, 1, null, null], 24));
            Assert.False(TranscodeTestRules.IsCollapse([0, 1, double.NaN, double.PositiveInfinity], 24));
            Assert.False(TranscodeTestRules.IsCollapse([-1, -1], 24));
            Assert.False(TranscodeTestRules.IsCollapse([6, 6], 24)); // At exactly a quarter is not under a quarter.
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(null)]
        public void IsCollapse_NeedsAtLeastTwoMeasuredStreams(double? other)
        {
            double?[] fps = [0, other];
            Assert.Equal(other.HasValue, TranscodeTestRules.IsCollapse(fps, 60));
            Assert.False(TranscodeTestRules.IsCollapse([0.0], 60));
        }

        [Fact]
        public void DescribeResult_SaysWhenTheStreamsCollapsedTogether()
        {
            var result = new TranscodeTestStressResult { EndReason = TranscodeTestEndReason.LimitReached, MaxStableStreams = 8, StreamsAtEnd = 9, FailedStreamId = 2, FailedStreamFps = 0, Collapsed = true };
            Assert.Equal(
                "Result: 8 stream(s) kept up with real time. With 9 streams running, stream 2 fell to 0.0 fps. All streams slowed down sharply at once, rather than one falling a little behind. This does not identify the cause; check GPU memory, drivers, CPU load and storage.",
                TranscodeTestRules.DescribeResult(result));
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

        [Fact]
        public void EndEvidence_IsTheCauseOrFfmpegsErrorLines()
        {
            const TranscodeTestEndKind Exited = TranscodeTestEndKind.Exited;
            const TranscodeTestEndKind Stalled = TranscodeTestEndKind.Stalled;

            // A plain failure keeps its error lines, even when empty.
            Assert.Equal(string.Empty, TranscodeTestRules.EndEvidence(Exited, TranscodeTestEndReason.StreamFailed, false, null, null, []));
            Assert.Equal("err | more", TranscodeTestRules.EndEvidence(Exited, TranscodeTestEndReason.StreamFailed, false, null, Gpu, ["err", "more"]));

            // A stream that never started shows its error lines, or null; its other output is never the error.
            Assert.Equal("err", TranscodeTestRules.EndEvidence(Exited, TranscodeTestEndReason.StreamFailed, true, null, null, ["err"]));
            Assert.Null(TranscodeTestRules.EndEvidence(Exited, TranscodeTestEndReason.StreamFailed, true, null, null, []));
            Assert.Null(TranscodeTestRules.EndEvidence(Stalled, TranscodeTestEndReason.StreamStalled, true, null, null, []));
            Assert.Null(TranscodeTestRules.EndEvidence(Stalled, TranscodeTestEndReason.StreamStalled, false, null, null, []));

            // The driver limit and full GPU memory show only the line that names them; the rest stays in the log.
            Assert.Equal(Session, TranscodeTestRules.EndEvidence(Exited, TranscodeTestEndReason.NvencSessionRefused, false, Session, null, ["late"]));
            Assert.Equal(Session, TranscodeTestRules.EndEvidence(Exited, TranscodeTestEndReason.NvencSessionRefused, false, Session, null, [Session, "late"]));
            Assert.Equal(Gpu, TranscodeTestRules.EndEvidence(Stalled, TranscodeTestEndReason.GpuMemoryFull, false, null, Gpu, []));
            Assert.Equal(Gpu, TranscodeTestRules.EndEvidence(Exited, TranscodeTestEndReason.GpuMemoryFull, false, null, Gpu, ["late", "later"]));
        }
    }
}
