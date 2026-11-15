using System;
using System.Collections.Generic;
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
    /// What a stalled stream keeps of ffmpeg's output, and how a stall is concluded.
    /// </summary>
    public class TranscodeTestStreamTests
    {
        private static readonly string?[] _encoders = ["h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_videotoolbox", "h264_qsv", "hevc_vaapi", "libx264", null];

        private static readonly string[] _evidence =
        [
            string.Empty,
            "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)",
            "[hevc_nvenc @ 0x0] OpenEncodeSessionEx failed: incompatible client key (21)",
            "Stream mapping: Stream #0:0 -> #0:0 (hevc (native) -> h264 (h264_nvenc))",
            "[hls @ 0x0] Opening 'segment0.ts' for writing",
        ];

        private static readonly string[] _lines =
        [
            "frame=  120 fps= 60 q=-0.0 size=N/A time=00:00:05.00 bitrate=N/A speed=2.5x",
            "Stream mapping:",
            "[h264 @ 0x0] Error while decoding stream #0:0",
            "  Stream #0:0 -> #0:0 (hevc (native) -> h264 (h264_nvenc))",
            "   ",
            string.Empty,
            "Press [q] to stop, [?] for help",
        ];

        [Fact]
        public void SelectEvidence_PrefersTheErrorLines()
            => Assert.Equal("Conversion failed!", TranscodeTestStream.SelectEvidence("Conversion failed!", ["a", "b"]));

        [Fact]
        public void SelectEvidence_FallsBackToTheLastLines()
            => Assert.Equal("a | b", TranscodeTestStream.SelectEvidence(string.Empty, ["a", "b"]));

        [Fact]
        public void SelectEvidence_IsEmptyWhenFfmpegSaidNothing()
            => Assert.Equal(string.Empty, TranscodeTestStream.SelectEvidence("  ", []));

        [Theory]
        [InlineData("[h264_nvenc @ 0x0] [error] OpenEncodeSessionEx failed: incompatible client key (21)", "error", "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: incompatible client key (21)")]
        [InlineData("[vost#0:0/h264_nvenc @ 0x1] [enc:h264_nvenc @ 0x2] [fatal] No capable devices found", "fatal", "[vost#0:0/h264_nvenc @ 0x1] [enc:h264_nvenc @ 0x2] No capable devices found")]
        [InlineData("[info] Conversion failed!", "info", "Conversion failed!")]
        [InlineData("[h264_nvenc @ 0x0] [info] [ GPU #0 - < NVIDIA GeForce RTX 3060 > has Compute SM 8.6 ]", "info", "[h264_nvenc @ 0x0] [ GPU #0 - < NVIDIA GeForce RTX 3060 > has Compute SM 8.6 ]")]
        [InlineData("[info] frame=   48 fps= 24 q=-0.0 size=N/A", "info", "frame=   48 fps= 24 q=-0.0 size=N/A")]
        [InlineData("x264 [error]: invalid preset 'nonsense'", null, "x264 [error]: invalid preset 'nonsense'")]
        [InlineData("[h264 @ 0x0] Error while decoding stream #0:0", null, "[h264 @ 0x0] Error while decoding stream #0:0")]
        public void SplitLevel_RemovesFfmpegsLevelTag(string line, string? level, string text)
            => Assert.Equal((level, text), TranscodeTestStream.SplitLevel(line));

        [Theory]
        // Tagged lines: the level decides, whatever the words say.
        [InlineData("[in#0 @ 0x0] [warning] Invalid Block Addition value 0x0 for unknown Block Addition Mapping type 68766345", false)]
        [InlineData("[libx264 @ 0x0] [warning] Error parsing option 'keyint = abc'.", false)]
        [InlineData("[info] Conversion failed!", false)]
        [InlineData("[AVFilterGraph @ 0x0] [error] No such filter: 'nosuchfilter'", true)]
        [InlineData("[fatal] Error opening output files: Filter not found", true)]
        // Untagged lines, printed by libraries straight to stderr: the words decide.
        [InlineData("x264 [error]: invalid preset 'nonsense'", true)]
        [InlineData("Stream mapping:", false)]
        public void LastError_KeepsOnlyErrors(string line, bool isError)
        {
            var stream = NewStream();
            stream.OnLine(line, 0);
            Assert.Equal(isError, stream.LastError.Length > 0);
        }

        [Fact]
        public void LastError_KeepsWholeLinesEvenWhenTheyAreLong()
        {
            var lines = TranscodeTestEndClassificationTests.LongErrors(6).Select(l => "[vost#0:0/h264_nvenc @ 0x0] [enc:h264_nvenc @ 0x1] [error] " + l).ToArray();
            var stream = NewStream();
            foreach (var line in lines)
            {
                stream.OnLine(line, 0);
            }

            var untagged = lines.Select(l => TranscodeTestStream.SplitLevel(l).Text).ToArray();
            Assert.Equal(untagged[2..], stream.ErrorLines);
            Assert.Equal(string.Join(" | ", untagged[2..]), stream.LastError);
            Assert.All(stream.LastError.Split(" | "), part => Assert.StartsWith("[vost#0:0/h264_nvenc @ 0x0] [enc:h264_nvenc @ 0x1] ", part, StringComparison.Ordinal));
        }

        [Fact]
        public void LastError_LeavesOutARepeatOfTheLineBefore()
        {
            var stream = NewStream();
            foreach (var line in new[] { "[h264 @ 0x0] Error A", "[h264 @ 0x0] Error A", "  [h264 @ 0x0] Error A ", "[h264 @ 0x0] Error B", "[h264 @ 0x0] Error A" })
            {
                stream.OnLine(line, 0);
            }

            Assert.Equal("[h264 @ 0x0] Error A | [h264 @ 0x0] Error B | [h264 @ 0x0] Error A", stream.LastError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GpuMemoryCause_IsShownOnceAfterFiveMoreErrors(bool exited)
        {
            string[] lines = [TranscodeTestEndClassificationTests.CudaLine, .. TranscodeTestEndClassificationTests.LongErrors(5)];
            var stream = TranscodeTestEndClassificationTests.Feed(lines);
            var kind = exited ? TranscodeTestEndKind.Exited : TranscodeTestEndKind.Stalled;
            var (reason, couldNotStart) = TranscodeTestRules.EndOutcome(kind, "av1_nvenc", stream.SessionLimitLine, stream.GpuMemoryLine, 3, true, false, 3);
            var error = TranscodeTestRules.EndEvidence(kind, reason, couldNotStart, stream.SessionLimitLine, stream.GpuMemoryLine, stream.ErrorLines);
            var logged = TranscodeTestRules.WithCause(TranscodeTestRules.EndCause(reason, stream.SessionLimitLine, stream.GpuMemoryLine), stream.ErrorLines);

            Assert.Equal(TranscodeTestEndReason.GpuMemoryFull, reason);
            Assert.Equal(TranscodeTestEndClassificationTests.CudaLine, error);
            Assert.Equal(1, Occurrences(logged, TranscodeTestEndClassificationTests.CudaLine));
            Assert.StartsWith(TranscodeTestEndClassificationTests.CudaLine + " | ", logged, StringComparison.Ordinal);
            Assert.Equal(5, logged.Split(" | ").Length);
        }

        [Fact]
        public void GpuMemoryCause_StillInTheKeptLines_IsShownOnce()
        {
            string[] lines = [TranscodeTestEndClassificationTests.CudaLine, "Conversion failed!"];
            var stream = TranscodeTestEndClassificationTests.Feed(lines);
            var logged = TranscodeTestRules.WithCause(stream.GpuMemoryLine, stream.ErrorLines);
            Assert.Equal(TranscodeTestEndClassificationTests.CudaLine + " | Conversion failed!", logged);
        }

        [Fact]
        public void TaggedProgressLines_AreReadAsFrames()
        {
            var stream = NewStream();
            stream.OnLine("[info] frame=    0 fps=0.0 q=0.0 size=N/A", 0);
            Assert.False(stream.ReportedFrames);
            stream.OnLine("[info] frame=   48 fps= 24 q=-0.0 size=N/A", 2);
            stream.OnLine("[info] frame=   96 fps= 24 q=-0.0 size=N/A", 4);
            stream.OnLine("[info] frame=  144 fps= 24 q=-0.0 size=N/A", 6);
            Assert.True(stream.ReportedFrames);
            Assert.Equal(24, stream.Fps(6)!.Value, 1);
            Assert.Equal(string.Empty, stream.StallEvidence);
        }

        [Fact]
        public void Fps_StartedThenSilent_IsZero()
        {
            var stream = NewStream();
            stream.OnLine("frame=   24 fps= 24", 1);
            stream.OnLine("frame=   72 fps= 24", 3);

            Assert.Equal(24, stream.Fps(4)!.Value, 1);
            Assert.Equal(0, stream.Fps(10));
        }

        [Fact]
        public void Fps_NewProcess_IsUnmeasuredUntilAFullWindowAfterItsFirstFrame()
        {
            var stream = NewStream();
            stream.OnLine("frame=    0 fps=0.0", 0.5);
            // The encoder starts slowly, then runs at 24 fps.
            stream.OnLine("frame=    2 fps=0.0", 1);
            stream.OnLine("frame=    6 fps=4.0", 1.5);
            for (var t = 2.0; t <= 6; t += 0.5)
            {
                stream.OnLine(FormattableString.Invariant($"frame= {6 + (int)((t - 1.5) * 24)} fps= 24"), t);
                if (t < 4)
                {
                    Assert.Null(stream.Fps(t));
                }
            }

            // Only frames after the first count, so the slow start does not drag the first reading down.
            Assert.Equal(24, stream.Fps(6)!.Value, 1);
        }

        [Fact]
        public void Fps_QuietSoonAfterTheFirstFrame_IsStillZero()
        {
            var stream = NewStream();
            stream.OnLine("frame=   12 fps= 24", 1);
            stream.OnLine("frame=   24 fps= 24", 1.5);

            Assert.Null(stream.Fps(2));
            Assert.Equal(0, stream.Fps(5));
        }

        [Fact]
        public void Fps_RestartedStream_WaitsForItsOwnWindow()
        {
            var ended = NewStream();
            ended.OnLine("frame=   24 fps= 24", 1);
            ended.OnLine("frame=   96 fps= 24", 4);
            ended.OnLine("frame=  120 fps= 24", 5);
            Assert.NotNull(ended.Fps(5));

            var restarted = NewStream();
            restarted.CarryOver(ended);
            restarted.OnLine("frame=    6 fps= 0.0", 6);
            restarted.OnLine("frame=   30 fps= 24", 7);

            Assert.Null(restarted.Fps(7));
        }

        [Fact]
        public void Fps_NeverReportedAFrame_StaysUnmeasured()
        {
            var stream = NewStream();
            stream.OnLine("frame=    0 fps=0.0", 1);
            stream.OnLine("frame=    0 fps=0.0", 3);

            Assert.False(stream.ReportedFrames);
            Assert.Null(stream.Fps(10));
        }

        [Fact]
        public void StallEvidence_KeepsTheLastFiveLinesThatAreNotProgress()
        {
            var stream = NewStream();
            foreach (var line in new[] { "one", "frame=  10 fps=0.0", "two", "  ", "three", "four", "five", "six", "frame=  20 fps=1.0" })
            {
                stream.OnLine(line, 0);
            }

            Assert.Equal(string.Empty, stream.LastError);
            Assert.Equal("two | three | four | five | six", stream.StallEvidence);
        }

        [Fact]
        public void FirstFrameAt_IsWhenTheFirstFrameWasReported()
        {
            var stream = NewStream();
            stream.OnLine("frame=    0 fps=0.0", 1);
            Assert.Null(stream.FirstFrameAt);
            stream.OnLine("frame=    5 fps=0.0", 2.5);
            stream.OnLine("frame=   50 fps= 24", 4);

            Assert.Equal(2.5, stream.FirstFrameAt);
        }

        [Fact]
        public void StallDiagnostics_WhenFfmpegPrintedNothing_IsOneLine()
        {
            var stream = NewStream();
            Assert.True(stream.PrintedNothing);
            Assert.Equal("Stream 1 printed nothing in 30.0 s (CPU time 1.0 s, 0 B read).", stream.StallDiagnostics(30, TimeSpan.FromSeconds(1), 0));
            Assert.Equal("Stream 1 printed nothing in 30.0 s (CPU time unknown).", stream.StallDiagnostics(30, null, null));
        }

        [Fact]
        public void StallDiagnostics_SayHowFarTheStreamGot()
        {
            var stream = NewStream();
            stream.OnLine("[info] Input #0, matroska,webm, from 'movie.mkv':", 1);
            Assert.False(stream.PrintedNothing);
            Assert.True(stream.SawInput);
            Assert.Equal(
                "Stream 1 after 30.0 s: opened the file: yes; reached stream mapping: no; last output 29.0 s ago; CPU time unknown.",
                stream.StallDiagnostics(30, null, null));

            stream.OnLine("[info] Stream mapping:", 2);
            stream.OnLine("[info] frame=    0 fps=0.0", 4);

            Assert.Equal(
                "Stream 1 after 30.0 s: opened the file: yes; reached stream mapping: yes; last output 26.0 s ago; CPU time 1.5 s; 850 MB read.",
                stream.StallDiagnostics(30, TimeSpan.FromSeconds(1.5), 850_000_000));
        }

        [Fact]
        public void StallDiagnostics_BeforeTheFileIsOpened_SaysSo()
        {
            var stream = NewStream();
            stream.OnLine("[warning] some warning", 25.8);

            Assert.False(stream.SawInput);
            Assert.Equal(
                "Stream 1 after 30.0 s: opened the file: no; reached stream mapping: no; last output 4.2 s ago; CPU time 1.0 s; 1.02 GB read.",
                stream.StallDiagnostics(30, TimeSpan.FromSeconds(1), 1_020_000_000));
        }

        [Fact]
        public void DescribeFirstFrame_SplitsAtTheTimeTheFileWasOpened()
        {
            var stream = new TranscodeTestStream(5, new Process(), "/tmp", "p_", 24f, 48, 6);
            Assert.Null(stream.DescribeFirstFrame(null));

            stream.OnLine("[info] Input #0, matroska,webm, from 'movie.mkv':", 54);
            stream.OnLine("[info] Input #0, again", 55);
            stream.OnLine("[info] frame=    0 fps=0.0", 55.5);
            stream.OnLine("[info] frame=    3 fps=0.0", 56.7);

            Assert.Equal("Stream 5: opened the file within 6.0 s; first frame 2.7 s later.", stream.DescribeFirstFrame(null));
            Assert.Equal("Stream 5: opened the file within 6.0 s; first frame 2.7 s later (1.02 GB read).", stream.DescribeFirstFrame(1_020_000_000));
        }

        [Fact]
        public void DescribeFirstFrame_WithoutTheInputLine_KeepsTheSingleTime()
        {
            var stream = new TranscodeTestStream(2, new Process(), "/tmp", "p_", 24f, 12, 6);
            stream.OnLine("frame=    5 fps=0.0", 18.1);

            Assert.Equal("Stream 2: first frame within 6.1 s.", stream.DescribeFirstFrame(null));
            Assert.Equal("Stream 2: first frame within 6.1 s (850 MB read).", stream.DescribeFirstFrame(850_000_000));
        }

        [Fact]
        public void StressLogArguments_HideTheBannerAndTagTheLevel()
        {
            Assert.StartsWith("-hide_banner ", TranscodeTestManager.StressLogArguments, StringComparison.Ordinal);
            Assert.Contains("-loglevel +level", TranscodeTestManager.StressLogArguments, StringComparison.Ordinal);
        }

        [Fact]
        public void StallEvidence_IsTheErrorWhenThereIsOne()
        {
            var stream = NewStream();
            stream.OnLine("Stream mapping:", 0);
            stream.OnLine("[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)", 0);
            stream.OnLine("Waiting for the device", 0);

            Assert.Equal("[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)", stream.StallEvidence);
        }

        [Property]
        public Property StallEvidence_IsLastErrorOrTheLastFiveLines(byte[] picks, string?[] extra)
        {
            var lines = Interleave(picks, extra);
            var stream = NewStream();
            foreach (var line in lines)
            {
                stream.OnLine(line, 0);
            }

            var tail = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !IsProgress(l)).Select(l => l.Trim()).TakeLast(5);
            var expected = string.IsNullOrWhiteSpace(stream.LastError) ? string.Join(" | ", tail) : stream.LastError;
            return (stream.StallEvidence == expected).Label("evidence is LastError, else the last five lines");
        }

        [Property]
        public Property StallOutcome_MatchesTheDesign(byte encoder, byte evidence, byte maxStable, bool isNewest)
        {
            var enc = _encoders[encoder % _encoders.Length];
            var ev = _evidence[evidence % _evidence.Length];
            var k = maxStable % 33;
            var (reason, couldNotStart) = TranscodeTestRules.StallOutcome(enc, ev, k, isNewest, 12);

            // Independent fixture oracle: entry 1 is allocation failure, entry 2 is a license restriction.
            // Neither, even at twelve other streams, proves a numerical driver session cap.
            var isNvenc = enc is "h264_nvenc" or "hevc_nvenc" or "av1_nvenc";
            var expectedReason = (isNvenc, evidence % _evidence.Length) switch
            {
                (true, 1) => TranscodeTestEndReason.NvencSessionRefused,
                (true, 2) => TranscodeTestEndReason.NvencSessionRefused,
                _ => TranscodeTestEndReason.StreamStalled
            };
            var expectedCouldNotStart = expectedReason == TranscodeTestEndReason.StreamStalled && isNewest && k >= 1;
            var expectedSurvey = expectedReason == TranscodeTestEndReason.StreamStalled
                ? TranscodeTestSurveyFailureReason.FailedTimeout
                : TranscodeTestSurveyFailureReason.FailedNvencLimit;

            return (reason == expectedReason).Label("reason")
                .And(couldNotStart == expectedCouldNotStart).Label("could not start")
                .And(TranscodeTestRules.SurveyFailureReason(reason) == expectedSurvey).Label("survey name");
        }

        private static TranscodeTestStream NewStream()
            => new(1, new Process(), "/tmp", "p_", 24f, 0, 6);

        private static bool IsProgress(string line)
            => System.Text.RegularExpressions.Regex.IsMatch(line, @"^frame=\s*\d+");

        private static List<string> Interleave(byte[] picks, string?[] extra)
        {
            var lines = new List<string>();
            for (var i = 0; i < picks.Length; i++)
            {
                lines.Add(_lines[picks[i] % _lines.Length]);
                if (i < extra.Length)
                {
                    lines.Add(extra[i] ?? string.Empty);
                }
            }

            return lines;
        }

        private static int Occurrences(string text, string part)
        {
            var count = 0;
            for (var i = text.IndexOf(part, StringComparison.Ordinal); i >= 0; i = text.IndexOf(part, i + part.Length, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }
    }
}
