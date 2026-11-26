using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// NVIDIA's resource and license errors cannot prove a numerical session cap, regardless of the count.
    /// </summary>
    public class TranscodeTestNvencRefusalTests
    {
        private const string OutOfMemory = "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)";

        private const string ClientKey = "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: incompatible client key (21): (no details)";

        [Theory]
        [InlineData(OutOfMemory, 0)]
        [InlineData(OutOfMemory, 3)]
        [InlineData(OutOfMemory, 5)]
        [InlineData(OutOfMemory, 8)]
        [InlineData(OutOfMemory, 12)]
        [InlineData(ClientKey, 0)]
        [InlineData(ClientKey, 3)]
        [InlineData(ClientKey, 5)]
        [InlineData(ClientKey, 8)]
        [InlineData(ClientKey, 12)]
        public void StallOutcome_AllocationAndLicenseFailuresNeverProveANumericalCap(string line, int otherSessions)
        {
            var (reason, couldNotStart) = TranscodeTestRules.StallOutcome("h264_nvenc", line, otherSessions, true, otherSessions);
            Assert.Equal(TranscodeTestEndReason.NvencSessionRefused, reason);
            Assert.False(couldNotStart);
        }

        [Theory]
        [InlineData(OutOfMemory, 3, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(OutOfMemory, 5, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(OutOfMemory, 8, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(OutOfMemory, 12, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(OutOfMemory, 7, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(ClientKey, 0, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(ClientKey, 3, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(ClientKey, 5, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(ClientKey, 8, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(ClientKey, 12, TranscodeTestEndReason.NvencSessionRefused)]
        [InlineData(ClientKey, 7, TranscodeTestEndReason.NvencSessionRefused)]
        public void EndOutcome_AllocationAndLicenseErrorsRemainNeutralAtEveryCount(string line, int otherSessions, TranscodeTestEndReason expected)
        {
            var (reason, couldNotStart) = TranscodeTestRules.EndOutcome(TranscodeTestEndKind.Exited, "h264_nvenc", line, null, otherSessions, true, false, otherSessions);

            Assert.Equal(expected, reason);
            Assert.False(couldNotStart);
            Assert.Equal(line, TranscodeTestRules.EndCause(reason, line, null));
            Assert.Equal(TranscodeTestSurveyFailureReason.FailedNvencLimit, TranscodeTestRules.SurveyFailureReason(reason));
        }

        [Fact]
        public void DescribeNvencRefusal_HintsAtTheSessionLimitOnlyForCode21()
        {
            Assert.Equal(
                "NVENC refused a new session for stream 4; this does not identify the cause, see ffmpeg's diagnostic above. 3 streams kept up with real time before that.",
                TranscodeTestRules.DescribeNvencRefusal(4, 3, OutOfMemory));
            Assert.EndsWith(" " + TranscodeTestRules.NvencCode21Hint, TranscodeTestRules.DescribeNvencRefusal(4, 3, ClientKey), System.StringComparison.Ordinal);
            Assert.DoesNotContain("Code 21", TranscodeTestRules.DescribeNvencRefusal(1, 0, null), System.StringComparison.Ordinal);
        }

        [Fact]
        public void EndOutcome_RefusalWithAnotherEncoder_IsNoNvidiaResult()
        {
            var (reason, _) = TranscodeTestRules.EndOutcome(TranscodeTestEndKind.Exited, "h264_qsv", OutOfMemory, null, 7, true, false, 7);

            Assert.Equal(TranscodeTestEndReason.StreamFailed, reason);
        }
    }
}
