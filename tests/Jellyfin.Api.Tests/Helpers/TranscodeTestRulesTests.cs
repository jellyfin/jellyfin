using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using MediaBrowser.Model.Configuration;
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
        [InlineData(true, 1, null, true, TranscodeTestOutcome.Failed, TranscodeTestAdvice.TurnOff)]
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
        [InlineData("hevc_qsv", 1, true, true, TranscodeTestOutcome.Failed, TranscodeTestAdvice.TurnOff)]
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
        [InlineData(true, true, 0, TranscodeTestOutcome.NotSupported)] // No frame, the other half ruled out, and H.264 passed.
        [InlineData(false, true, 0, TranscodeTestOutcome.Failed)] // H.264 failed or was not tested: the setup may be broken.
        [InlineData(true, true, null, TranscodeTestOutcome.Failed)] // ffmpeg did not say how many frames.
        [InlineData(true, true, 5, TranscodeTestOutcome.Failed)] // Frames got through, so something else failed.
        [InlineData(true, false, 0, TranscodeTestOutcome.Failed)] // Decoding failed with the GPU encoder, which is not ruled out.
        public void FailedOutcome_IsNotSupportedOnlyForNoFramesAfterAPassedReference(bool referencePassed, bool otherHalfRuledOut, int? frames, TranscodeTestOutcome expected)
            => Assert.Equal(expected, TranscodeTestRules.FailedOutcome(referencePassed, otherHalfRuledOut, frames));

        [Theory]
        [InlineData(0, true, TranscodeTestOutcome.NotSupported, false)]
        [InlineData(0, false, TranscodeTestOutcome.Failed, false)]
        [InlineData(null, true, TranscodeTestOutcome.Failed, false)]
        [InlineData(12, true, TranscodeTestOutcome.Failed, true)] // Still reported as failing after decoding.
        public void FailedDecode_AfterTheCpuEncoderRetry_IsConcludedFromTheFramesAndTheReference(int? frames, bool referencePassed, TranscodeTestOutcome outcome, bool failedAfterDecoding)
        {
            var (failed, advice) = TranscodeTestRules.Decode(true, 1, null, true);

            Assert.Equal(TranscodeTestOutcome.Failed, failed);
            Assert.Equal(outcome, TranscodeTestRules.FailedOutcome(referencePassed, true, frames));
            Assert.Equal(failedAfterDecoding, TranscodeTestRules.FailedAfterDecoding(frames, null));
            Assert.Equal(TranscodeTestAdvice.TurnOff, advice); // The advice to turn it off stays either way.
        }

        [Theory]
        [InlineData("Output stream #0:0 (video): 0 frames encoded; 0 packets muxed (0 bytes);", true, TranscodeTestOutcome.NotSupported)]
        [InlineData("Output stream #0:0 (video): 0 frames encoded; 0 packets muxed (0 bytes);", false, TranscodeTestOutcome.Failed)]
        [InlineData("[av1_nvenc @ 0x0] Error while opening encoder", true, TranscodeTestOutcome.Failed)]
        [InlineData("Output stream #0:0 (video): 40 frames encoded; 40 packets muxed (9000 bytes);", true, TranscodeTestOutcome.Failed)]
        public void FailedEncode_IsConcludedFromTheFramesEncodedAndTheReference(string output, bool referencePassed, TranscodeTestOutcome outcome)
        {
            var (failed, advice) = TranscodeTestRules.Encode("av1_nvenc", 1, true, true);

            Assert.Equal(TranscodeTestOutcome.Failed, failed);
            Assert.Equal(outcome, TranscodeTestRules.FailedOutcome(referencePassed, true, TranscodeTestRules.FramesEncoded(output)));
            Assert.Equal(TranscodeTestAdvice.TurnOff, advice);
        }

        [Theory]
        [InlineData(Rtx3070Av1Output, true, TranscodeTestOutcome.NotSupported)]
        [InlineData(Rtx3070Av1Output, false, TranscodeTestOutcome.Failed)] // H.264 encoding did not pass.
        [InlineData("frame=    0 fps=0.0 q=0.0 size=       0KiB\r" + Rtx3070Av1Output, true, TranscodeTestOutcome.NotSupported)]
        [InlineData("[av1_nvenc @ 0x0] Error while opening encoder", true, TranscodeTestOutcome.NotSupported)]
        [InlineData("[vost#0:0/av1_nvenc @ 0x0] [enc:av1_nvenc @ 0x1] Could not open encoder before EOF", true, TranscodeTestOutcome.NotSupported)]
        [InlineData("Output stream #0:0 (video): 0 frames encoded; 0 packets muxed (0 bytes);", true, TranscodeTestOutcome.NotSupported)]
        [InlineData("Output stream #0:0 (video): 12 frames encoded; 12 packets muxed (9000 bytes);\n" + Rtx3070Av1Output, true, TranscodeTestOutcome.Failed)]
        [InlineData("frame=   12 fps=0.0 q=0.0 size=       0KiB\r" + Rtx3070Av1Output, true, TranscodeTestOutcome.Failed)] // Frames got out first.
        [InlineData("[vost#0:1/aac @ 0x0] Task finished with error code: -22 (Invalid argument)\nConversion failed!", true, TranscodeTestOutcome.Failed)]
        [InlineData("[vost#0:0/h264_nvenc @ 0x0] Error while opening encoder\nConversion failed!", true, TranscodeTestOutcome.Failed)]
        [InlineData("[hevc @ 0x0] Error while decoding stream #0:0\nConversion failed!", true, TranscodeTestOutcome.Failed)]
        public void FailedEncode_WithoutAFrameCount_IsNotSupportedOnlyWhenThisEncoderFailedBeforeAnyOutput(string output, bool referencePassed, TranscodeTestOutcome outcome)
        {
            var (failed, advice) = TranscodeTestRules.Encode("av1_nvenc", 1, true, true);

            Assert.Equal(TranscodeTestOutcome.Failed, failed);
            Assert.Equal(outcome, TranscodeTestRules.FailedOutcome(referencePassed, true, TranscodeTestRules.EncodedFrames(output, "av1_nvenc")));
            Assert.Equal(TranscodeTestAdvice.TurnOff, advice);
        }

        [Theory]
        [InlineData("tonemap_opencl=format=nv12", 0, false, TranscodeTestOutcome.Passed, TranscodeTestAdvice.TurnOn)]
        [InlineData("tonemapx=tonemap=bt2390", 0, true, TranscodeTestOutcome.Software, TranscodeTestAdvice.None)]
        [InlineData("tonemap_cuda=format=yuv420p", 1, true, TranscodeTestOutcome.Failed, TranscodeTestAdvice.TurnOff)]
        [InlineData(null, null, false, TranscodeTestOutcome.NotSupported, TranscodeTestAdvice.None)]
        public void Tonemap_ReportsWhereItRuns(string? filter, int? exitCode, bool enabled, TranscodeTestOutcome outcome, TranscodeTestAdvice advice)
            => Assert.Equal((outcome, advice), TranscodeTestRules.Tonemap(filter, exitCode, enabled));
    }
}
