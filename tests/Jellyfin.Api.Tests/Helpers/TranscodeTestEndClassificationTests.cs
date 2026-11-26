using System;
using System.Diagnostics;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// How the stream that ends a stress test is classified: NVIDIA's session refusal anywhere in its output, the GPU
    /// running out of memory, and the newest stream exiting before its first frame (jellyfin-meta #163).
    /// </summary>
    public class TranscodeTestEndClassificationTests
    {
        internal const string GauvinoRefusal = "[h264_nvenc @ 0x776e86ebba80] OpenEncodeSessionEx failed: incompatible client key (21): (no details)";

        internal const string CudaLine = "[av1 @ 0x0] cuvidCreateDecoder(&ctx->cudecoder, &cuinfo) failed -> CUDA_ERROR_OUT_OF_MEMORY: out of memory";

        /// <summary>
        /// The refused 13th h264_nvenc session, verbatim from Gauvino's report on jellyfin-meta #163.
        /// The observed count does not establish the cause of the code 21 licensing diagnostic.
        /// </summary>
        internal static readonly string[] GauvinoStderr =
        [
            "[vost#0:0/h264_nvenc @ 0x776e86eaf500] Created video stream from input stream 0:0",
            "  Stream #0:0 -> #0:0 (wrapped_avframe (native) -> h264 (h264_nvenc))",
            "[vost#0:0/h264_nvenc @ 0x776e86eaf500] Starting thread...",
            "[h264_nvenc @ 0x776e86ebba80] Loaded Nvenc version 13.1",
            "[h264_nvenc @ 0x776e86ebba80] Nvenc initialized successfully",
            "[h264_nvenc @ 0x776e86ebba80] 1 CUDA capable devices found",
            "[h264_nvenc @ 0x776e86ebba80] [ GPU #0 - < NVIDIA GeForce RTX 3060 > has Compute SM 8.6 ]",
            GauvinoRefusal,
            "[h264_nvenc @ 0x776e86ebba80] No capable devices found",
            "[h264_nvenc @ 0x776e86ebba80] Nvenc unloaded",
            "[vost#0:0/h264_nvenc @ 0x776e86eaf500] [enc:h264_nvenc @ 0x776e86e0ac00] Error while opening encoder - maybe incorrect parameters such as bit_rate, rate, width or height.",
            "[vf#0:0 @ 0x776e86eca000] Error sending frames to consumers: Generic error in an external library",
            "[vf#0:0 @ 0x776e86eca000] Task finished with error code: -542398533 (Generic error in an external library)",
            "[vf#0:0 @ 0x776e86eca000] Terminating thread with return code -542398533 (Generic error in an external library)",
            "[vost#0:0/h264_nvenc @ 0x776e86eaf500] [enc:h264_nvenc @ 0x776e86e0ac00] Encoder thread received EOF",
            "[vost#0:0/h264_nvenc @ 0x776e86eaf500] [enc:h264_nvenc @ 0x776e86e0ac00] Could not open encoder before EOF",
            "[vost#0:0/h264_nvenc @ 0x776e86eaf500] Task finished with error code: -22 (Invalid argument)",
            "[vost#0:0/h264_nvenc @ 0x776e86eaf500] Terminating thread with return code -22 (Invalid argument)",
            "[in#0/lavfi @ 0x776e86e0e140]   Input stream #0:0 (video): 2 packets read (848 bytes); 1 frames decoded; 0 decode errors; ",
            "Conversion failed!",
        ];

        internal static readonly string[] Exit218Stderr =
        [
            "[hevc @ 0x0] hwaccel initialisation returned error.",
            "[hevc @ 0x0] Failed setup for format cuda: hwaccel initialisation returned error.",
            "Conversion failed!",
        ];

        [Fact]
        public void Gauvino_TheKeptLinesHaveLostTheRefusal()
        {
            var stream = Feed(GauvinoStderr);
            Assert.Equal(4, stream.ErrorLines.Count);
            Assert.All(stream.ErrorLines, line => Assert.Contains(line, GauvinoStderr.Select(l => l.Trim())));
            Assert.Equal("Conversion failed!", stream.ErrorLines[^1]);
            Assert.DoesNotContain(GauvinoRefusal, stream.LastError, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Gauvino_EndsAsANeutralRefusal_NotADefiniteCap(bool exited)
        {
            var (reason, couldNotStart, error) = Classify(exited, "h264_nvenc", GauvinoStderr, 12, true);
            Assert.Equal(TranscodeTestEndReason.NvencSessionRefused, reason);
            Assert.Equal(TranscodeTestSurveyFailureReason.FailedNvencLimit, TranscodeTestRules.SurveyFailureReason(reason));
            Assert.False(couldNotStart);
            Assert.Equal(GauvinoRefusal, error);
        }

        [Theory]
        [InlineData(true, 15)]
        [InlineData(false, 15)]
        [InlineData(true, 3)]
        [InlineData(false, 3)]
        public void CudaOutOfMemory_EndsAsGpuMemoryFull(bool exited, int maxStable)
        {
            var (reason, couldNotStart, error) = Classify(exited, "av1_nvenc", [CudaLine, .. LongErrors(10)], maxStable, true);
            Assert.Equal("GpuMemoryFull", reason.ToString());
            Assert.Null(TranscodeTestRules.SurveyFailureReason(reason));
            Assert.False(couldNotStart);
            Assert.Equal(CudaLine, error);
        }

        [Theory]
        [InlineData(22, "h264_nvenc")]
        [InlineData(3, "h264_nvenc")]
        [InlineData(2, "h264_nvenc")]
        [InlineData(22, "h264_videotoolbox")]
        public void EarlyExitOfTheNewest_IsACapacityResult(int maxStable, string encoder)
        {
            var (reason, couldNotStart, error) = Classify(true, encoder, Exit218Stderr, maxStable, true);
            Assert.Equal(TranscodeTestEndReason.StreamFailed, reason);
            Assert.Null(TranscodeTestRules.SurveyFailureReason(reason));
            Assert.True(couldNotStart);
            Assert.Contains("hwaccel initialisation returned error", error, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("h264_nvenc")]
        [InlineData("h264_videotoolbox")]
        public void SilentHangAfterAWarning_HasNoErrorAndNoNvidiaBlame(string encoder)
        {
            // EliasV21, RTX 5080: stream 5 hung for 30 s; its only output was a demuxer warning about the file.
            const string Warning = "[in#0 @ 0x758f2d0d4d80] Invalid Block Addition value 0x0 for unknown Block Addition Mapping type 68766345, name \"\"";
            string[] lines = ["[in#0 @ 0x758f2d0d4d80] [warning] Invalid Block Addition value 0x0 for unknown Block Addition Mapping type 68766345, name \"\""];
            var (reason, couldNotStart, error) = Classify(false, encoder, lines, 4, true);
            Assert.Equal(TranscodeTestEndReason.StreamStalled, reason);
            Assert.True(couldNotStart);
            Assert.Null(error);
            Assert.Equal(Warning, Feed(lines).StallEvidence);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GauvinoTagged_EndsAsANeutralRefusalWithTheUntaggedEvidence(bool exited)
        {
            // The same output as printed with "-loglevel +level": every line tagged, the refusal at error level.
            var tagged = GauvinoStderr.Select(l => l.Contains("rror", StringComparison.Ordinal) || l.Contains("ailed", StringComparison.Ordinal) || l.Contains("No capable", StringComparison.Ordinal)
                ? Tag(l, "error")
                : Tag(l, "info")).ToArray();
            var (reason, _, error) = Classify(exited, "h264_nvenc", tagged, 12, true);
            Assert.Equal(TranscodeTestEndReason.NvencSessionRefused, reason);
            Assert.Equal(GauvinoRefusal, Feed(tagged).SessionLimitLine);
            Assert.StartsWith(GauvinoRefusal, error, StringComparison.Ordinal);
            Assert.DoesNotContain("[error]", error, StringComparison.Ordinal);
        }

        [Fact]
        public void NeutralCode10_ErrorIsOnlyTheRefusal()
        {
            string[] lines = ["[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)", "Conversion failed!"];
            var (reason, _, error) = Classify(true, "h264_nvenc", lines, 4, true);
            Assert.Equal(TranscodeTestEndReason.NvencSessionRefused, reason);
            Assert.Equal(lines[0], error);
        }

        [Property]
        public Property LicenseRefusalFollowedByLongErrors_NeverProvesADefiniteCap(byte count, byte maxStable, bool exited, bool isNewest)
        {
            var lines = new[] { GauvinoRefusal }.Concat(LongErrors(5 + (count % 16))).ToArray();
            var stream = Feed(lines);
            var (reason, _, error) = Classify(exited, "hevc_nvenc", lines, maxStable % 33, isNewest);
            return (stream.SessionLimitLine == GauvinoRefusal).Label("the refusal is kept")
                .And(reason == TranscodeTestEndReason.NvencSessionRefused).Label("reason")
                .And(error == GauvinoRefusal).Label("the error is the refusal");
        }

        [Property(MaxTest = 1000)]
        public Property EveryNamedCauseEnding_NamesTheCause(byte[] picks, bool exited, byte encoder, byte maxStable, bool isNewest)
        {
            var (enc, lines, k) = TranscodeTestEndPreservationTests.Input(picks, encoder, maxStable);
            if (!TranscodeTestEndPreservationTests.IsNamedCauseEnding(exited, enc, lines, k, isNewest))
            {
                return true.ToProperty();
            }

            var stream = Feed(lines);
            var (reason, couldNotStart, error) = Classify(exited, enc, lines, k, isNewest);
            var survey = TranscodeTestRules.SurveyFailureReason(reason);
            if (TranscodeTestEndPreservationTests.IsSessionLimit(enc, lines))
            {
                var first = lines.First(TranscodeTestEndPreservationTests.SessionText).Trim();
                return (reason == TranscodeTestEndReason.NvencSessionRefused && !couldNotStart).Label("neutral refusal classification")
                    .And(survey == TranscodeTestSurveyFailureReason.FailedNvencLimit).Label("survey")
                    .And(error == first).Label("the error is the refusal");
            }

            if (TranscodeTestEndPreservationTests.IsGpuMemoryFull(enc, lines, k))
            {
                var first = lines.First(TranscodeTestEndPreservationTests.GpuMemoryLine).Trim();
                return (reason == TranscodeTestEndReason.GpuMemoryFull && !couldNotStart).Label("GPU memory full")
                    .And(survey is null).Label("survey")
                    .And(error == first).Label("the error is the memory line");
            }

            return (reason == TranscodeTestEndReason.StreamFailed && couldNotStart).Label("could not start")
                .And(survey is null).Label("survey")
                .And(error == (string.IsNullOrEmpty(stream.LastError) ? null : stream.LastError)).Label("error is ffmpeg's error lines");
        }

        [Fact]
        public void Gauvino_TheRefusalLineIsKept()
            => Assert.Equal(GauvinoRefusal, Feed(GauvinoStderr).SessionLimitLine);

        [Fact]
        public void ReportedFrames_NeedsAFrameNumberOfOneOrMore()
        {
            var stream = Feed(["frame=    0 fps=0.0 q=0.0 size=N/A time=N/A bitrate=N/A speed=N/A", "frame=    0 fps=0.0"]);
            Assert.False(stream.ReportedFrames);
            stream.OnLine("frame=    1 fps=0.5 q=20.0 size=N/A time=00:00:00.04 bitrate=N/A speed=0.02x", 1);
            Assert.True(stream.ReportedFrames);
            Assert.True(Feed([], reportFrames: true).ReportedFrames);
        }

        [Fact]
        public void TheFirstMatchedLinesAreKept()
        {
            const string Vaapi = "[hevc @ 0x0] Failed to create surface: 2 (resource allocation failed).";
            var stream = Feed(["  " + CudaLine + "  ", Vaapi, "[hevc_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10)", GauvinoRefusal]);
            Assert.Equal(CudaLine, stream.GpuMemoryLine);
            Assert.Equal("[hevc_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10)", stream.SessionLimitLine);
        }

        [Property]
        public Property GpuMemoryLineAfterOneKeptUp_IsGpuMemoryFull(byte maxStable, bool exited, bool isNewest, byte encoder)
        {
            string?[] encoders = ["h264_nvenc", "av1_nvenc", "h264_videotoolbox", "hevc_vaapi", "h264_qsv", null];
            var (reason, couldNotStart, _) = Classify(exited, encoders[encoder % encoders.Length], [CudaLine], 1 + (maxStable % 32), isNewest);
            return (reason.ToString() == "GpuMemoryFull").Label("reason")
                .And(!couldNotStart).Label("no capacity flag");
        }

        // Adds ffmpeg's level tag after a line's context prefixes, as "-loglevel +level" prints it.
        internal static string Tag(string line, string level)
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, @"^((?:\[[^\[\]]*\] )*)");
            return match.Groups[1].Value + "[" + level + "] " + line[match.Length..];
        }

        internal static string[] LongErrors(int count)
            => Enumerable.Range(0, count)
                .Select(i => $"[vf#0:0 @ 0x0] Error {i} while filtering: Generic error in an external library " + new string('x', 300 + ((i * 37) % 500)))
                .ToArray();

        internal static TranscodeTestStream Feed(string[] lines, bool reportFrames = false)
        {
            var stream = new TranscodeTestStream(1, new Process(), "/tmp", "p_", 24f, 0, 6);
            var now = 0.0;
            if (reportFrames)
            {
                stream.OnLine("frame=   48 fps= 24 q=-0.0 size=N/A time=00:00:02.00 bitrate=N/A speed=1x", now += 0.1);
            }

            foreach (var line in lines)
            {
                stream.OnLine(line, now += 0.1);
            }

            return stream;
        }

        /// <summary>
        /// Concludes the stream that ended the run the way the manager does.
        /// </summary>
        /// <param name="exited">Whether ffmpeg exited with a non-zero code; otherwise it stalled.</param>
        /// <param name="encoder">The streams' video encoder.</param>
        /// <param name="lines">ffmpeg's output for the stream.</param>
        /// <param name="maxStable">The most streams that all kept up.</param>
        /// <param name="isNewest">Whether the stream is the newest.</param>
        /// <param name="reportFrames">Whether to report one frame before the lines.</param>
        /// <param name="otherSessions">How many other streams ran; no count alone proves an NVENC session limit.</param>
        /// <returns>The end reason, the could-not-start flag and the error text.</returns>
        internal static (TranscodeTestEndReason Reason, bool CouldNotStart, string? Error) Classify(
            bool exited, string? encoder, string[] lines, int maxStable, bool isNewest, bool reportFrames = false, int otherSessions = 12)
        {
            var stream = Feed(lines, reportFrames);

            // The manager's EndStream.
            var kind = exited ? TranscodeTestEndKind.Exited : TranscodeTestEndKind.Stalled;
            var (reason, couldNotStart) = TranscodeTestRules.EndOutcome(kind, encoder, stream.SessionLimitLine, stream.GpuMemoryLine, maxStable, isNewest, stream.ReportedFrames, otherSessions);
            var error = TranscodeTestRules.EndEvidence(kind, reason, couldNotStart, stream.SessionLimitLine, stream.GpuMemoryLine, stream.ErrorLines);
            return (reason, couldNotStart, error);
        }
    }
}
