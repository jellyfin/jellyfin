using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// When NVIDIA's encoder refuses a session, the driver's session limit is blamed only when it fits: the refusal
    /// "out of memory (10)" is also what NVENC says when the GPU's memory is full.
    /// </summary>
    public class TranscodeTestNvencRefusalTests
    {
        private const string OutOfMemory = "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)";

        private const string ClientKey = "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: incompatible client key (21): (no details)";

        [Theory]
        [InlineData(OutOfMemory, 3, true)]
        [InlineData(OutOfMemory, 5, true)]
        [InlineData(OutOfMemory, 8, true)]
        [InlineData(OutOfMemory, 12, true)]
        [InlineData(OutOfMemory, 7, false)]
        [InlineData(OutOfMemory, 0, false)]
        [InlineData(ClientKey, 7, true)]
        [InlineData(ClientKey, 0, true)]
        public void IsKnownSessionLimit_NeedsCode21OrAKnownCount(string line, int otherSessions, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.IsKnownSessionLimit(line, otherSessions));

        [Theory]
        [InlineData(OutOfMemory, 12, TranscodeTestEndReason.NvencSessionLimit)]
        [InlineData(OutOfMemory, 7, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(ClientKey, 7, TranscodeTestEndReason.NvencSessionLimit)]
        public void EndOutcome_BlamesTheDriverOnlyWhenItFits(string line, int otherSessions, TranscodeTestEndReason expected)
        {
            var (reason, couldNotStart) = TranscodeTestRules.EndOutcome(TranscodeTestEndKind.Exited, "h264_nvenc", line, null, otherSessions, true, false, otherSessions);

            Assert.Equal(expected, reason);
            Assert.False(couldNotStart);
            Assert.Equal(line, TranscodeTestRules.EndCause(reason, line, null));
            Assert.Equal(TranscodeTestSurveyFailureReason.FailedNvencLimit, TranscodeTestRules.SurveyFailureReason(reason));
        }

        [Fact]
        public void EndOutcome_RefusalWithAnotherEncoder_IsNoNvidiaResult()
        {
            var (reason, _) = TranscodeTestRules.EndOutcome(TranscodeTestEndKind.Exited, "h264_qsv", OutOfMemory, null, 7, true, false, 7);

            Assert.Equal(TranscodeTestEndReason.StreamFailed, reason);
        }
    }
}
