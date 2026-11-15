using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// A compatibility check that failed because the GPU refused a session or ran out of memory, for example because
    /// playback held them, says nothing about the format: it is a failure, never "not supported".
    /// </summary>
    public class TranscodeTestCompatibilityFailureTests
    {
        private const string Refusal = "[hevc_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)";

        private const string CudaMemory = "[h264 @ 0x0] cuvidCreateDecoder(&ctx->cudecoder, &cuinfo) failed -> CUDA_ERROR_OUT_OF_MEMORY: out of memory";
        private const string Unsupported = "[hevc_nvenc @ 0x0] Codec not supported";

        [Fact]
        public void FailedOutcome_NoFramesAfterAPassedReference_NeedsExplicitFormatEvidence()
        {
            Assert.Equal(TranscodeTestOutcome.Failed, TranscodeTestRules.FailedOutcome(true, true, 0));
            Assert.Equal(TranscodeTestOutcome.NotSupported, TranscodeTestRules.FailedOutcome(true, true, 0, output: Unsupported));
        }

        [Theory]
        [InlineData("[hevc_nvenc @ 0x0] Error while opening encoder - maybe incorrect parameters such as bit_rate, rate, width or height.")]
        [InlineData("[vost#0:0/hevc_nvenc @ 0x0] Task finished with error code: -22 (Invalid argument)")]
        [InlineData("[hevc_nvenc @ 0x0] InitializeEncoder failed: invalid param (8): (no details)")]
        [InlineData("[hevc_nvenc @ 0x0] No capable devices found")]
        [InlineData("[hevc_nvenc @ 0x0] Provided device doesn't support required NVENC features")]
        [InlineData("[hevc_nvenc @ 0x0] RC lookahead not supported")]
        [InlineData("[hevc_nvenc @ 0x0] Using global_quality with nvenc is not supported. Use qp instead.")]
        [InlineData("[Parsed_scale_0 @ 0x0] Codec not supported")]
        [InlineData("[libx265 @ 0x0] Codec not supported")]
        [InlineData("Codec not supported")]
        public void FailedOutcome_GenericConfigurationAndDeviceErrors_RemainFailures(string line)
        {
            Assert.Null(TranscodeTestRules.UnsupportedFormatLine(line));
            Assert.Equal(TranscodeTestOutcome.Failed, TranscodeTestRules.FailedOutcome(true, true, 0, output: line));
        }

        [Theory]
        [InlineData(Unsupported)]
        [InlineData("[hevc_nvenc @ 0x0] [warning] Codec not supported")]
        [InlineData("[vost#0:0/av1_nvenc @ 0x0] [enc:av1_nvenc @ 0x1] Codec not supported")]
        [InlineData("[h264_nvenc @ 0x0] 10 bit encode not supported")]
        [InlineData("[hevc_nvenc @ 0x0] YUV444P not supported")]
        [InlineData("[hevc_nvenc @ 0x0] YUV422P not supported")]
        [InlineData("[hevc_cuvid @ 0x0] unsupported bit depth: 12")]
        [InlineData("[hevc_cuvid @ 0x0] Codec hevc_cuvid is not supported with this chroma format.")]
        [InlineData("[hevc_cuvid @ 0x0] Bit depth 12 with this chroma format is not supported.")]
        public void UnsupportedFormatLine_RequiresAnExplicitHardwareCapabilityDiagnostic(string line)
        {
            Assert.Equal(line, TranscodeTestRules.UnsupportedFormatLine(" \r\n " + line + "\r\nConversion failed!"));
            Assert.Equal(TranscodeTestOutcome.NotSupported, TranscodeTestRules.FailedOutcome(true, true, 0, output: line));
        }

        [Theory]
        [InlineData(Refusal)]
        [InlineData(CudaMemory)]
        public void FailedOutcome_WithASessionOrMemoryLine_IsAFailure(string line)
        {
            Assert.Equal(TranscodeTestOutcome.Failed, TranscodeTestRules.FailedOutcome(true, true, 0, line, Unsupported));
            Assert.Equal(TranscodeTestOutcome.Failed, TranscodeTestRules.FailedOutcome(true, true, 0, output: Unsupported + "\n" + line));
        }

        [Fact]
        public void ResourceLine_FindsTheRefusalOrMemoryLine()
        {
            Assert.Equal(Refusal, TranscodeTestRules.ResourceLine("[hevc_nvenc @ 0x0] Loaded Nvenc version 13.1\n  " + Refusal + "\r\nConversion failed!"));
            Assert.Equal(CudaMemory, TranscodeTestRules.ResourceLine("x\n" + CudaMemory));
            Assert.Null(TranscodeTestRules.ResourceLine("[av1_nvenc @ 0x0] Could not open encoder\nConversion failed!"));
        }

        [Theory]
        [InlineData(null, "a | b", "a | b")]
        [InlineData("cause", "a | b", "cause | a | b")]
        [InlineData("cause", "x cause | b", "x cause | b")]
        [InlineData("cause", "", "cause")]
        public void PrependCause_NamesTheCauseOnce(string? cause, string errors, string expected)
            => Assert.Equal(expected, TranscodeTestRules.PrependCause(cause, errors));
    }
}
