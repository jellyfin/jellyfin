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

        [Fact]
        public void FailedOutcome_NoFramesAfterAPassedReference_IsNotSupported()
            => Assert.Equal(TranscodeTestOutcome.NotSupported, TranscodeTestRules.FailedOutcome(true, true, 0));

        [Theory]
        [InlineData(Refusal)]
        [InlineData(CudaMemory)]
        public void FailedOutcome_WithASessionOrMemoryLine_IsAFailure(string line)
            => Assert.Equal(TranscodeTestOutcome.Failed, TranscodeTestRules.FailedOutcome(true, true, 0, line));

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
