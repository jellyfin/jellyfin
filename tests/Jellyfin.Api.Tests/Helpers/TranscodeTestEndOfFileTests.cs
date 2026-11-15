using System;
using System.Diagnostics;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// How a stream at the end of its file is restarted and judged: no restart loop, a restarted stream is not a stream
    /// that never started, and the drain at the end of the file is not a dip.
    /// </summary>
    public class TranscodeTestEndOfFileTests
    {
        [Theory]
        // Run time unknown: lifetime is not proof that the source reached EOF.
        [InlineData(0, true, 40, null, 0, false)]
        [InlineData(0, true, 40, null, TranscodeTestRules.MaxRestartsPerStream - 1, false)]
        [InlineData(0, true, 4, null, 0, false)]
        // Exited at once after a frame: too short to be the end of the file.
        [InlineData(0, true, 1.5, null, 0, false)]
        [InlineData(0, true, 2, null, 0, false)]
        // A fast encoder finished a short file in 2 s: its position, not its lifetime, says it reached the end.
        [InlineData(0, true, 2, true, 0, true)]
        // A long run that stopped far from the end is not the end of the file.
        [InlineData(0, true, 40, false, 0, false)]
        // ffmpeg exited cleanly without encoding: "Output file is empty, nothing was encoded".
        [InlineData(0, false, 40, null, 0, false)]
        [InlineData(0, false, 40, true, 0, false)]
        [InlineData(1, true, 40, true, 0, false)]
        [InlineData(1, true, 40, null, 0, false)]
        [InlineData(0, true, 40, true, TranscodeTestRules.MaxRestartsPerStream, false)]
        [InlineData(0, true, 40, null, TranscodeTestRules.MaxRestartsPerStream, false)]
        public void CanRestart_OnlyAStreamThatReallyPlayedToTheEnd(int exitCode, bool reportedFrames, double lived, bool? atEndOfFile, int restarts, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.CanRestart(exitCode, reportedFrames, lived, atEndOfFile, restarts));

        [Theory]
        [InlineData(13.5, 13.5, true)]
        [InlineData(13.4, 13.5, true)]
        // 327 real frames of a 13.5 s clip at 23.976 fps, without the duplicates ffmpeg may add.
        [InlineData(13.64, 13.5, true)]
        [InlineData(12.0, 13.5, true)]
        [InlineData(11.4, 13.5, false)]
        [InlineData(25.0, 25.0, true)]
        // A clean exit halfway through is not the end of the file.
        [InlineData(6.75, 13.5, false)]
        // 60 s of video in a 62 s container whose audio runs longer.
        [InlineData(60.35, 62.0, true)]
        [InlineData(59.9, 62.0, false)]
        [InlineData(null, 13.5, null)]
        [InlineData(13.5, null, null)]
        [InlineData(13.5, 0.0, null)]
        [InlineData(6481.0, 7200.0, false)] // Ninety percent of a two-hour file is not EOF.
        [InlineData(6552.0, 7200.0, false)] // Nor is 91 %.
        [InlineData(3600.0, 7200.0, false)]
        [InlineData(7189.9, 7200.0, false)]
        [InlineData(7190.0, 7200.0, true)]
        [InlineData(7199.0, 7200.0, true)]
        [InlineData(297.0, 300.0, true)]
        [InlineData(297.9, 300.0, true)]
        [InlineData(297.99, 300.0, true)]
        [InlineData(297.0, 300.5, false)]
        [InlineData(0.0, 0.5, false)]
        [InlineData(2.0, 4.0, false)] // Half of a 4 s clip.
        [InlineData(3.0, 4.0, true)]
        [InlineData(double.NaN, 13.5, null)]
        [InlineData(double.PositiveInfinity, 13.5, null)]
        [InlineData(13.5, double.NaN, null)]
        [InlineData(13.5, double.PositiveInfinity, null)]
        [InlineData(-1.0, 13.5, null)]
        public void IsAtEndOfFile_ComparesThePositionWithTheRunTime(double? position, double? source, bool? expected)
            => Assert.Equal(expected, TranscodeTestRules.IsAtEndOfFile(position, source));

        [Fact]
        public void DescribeNoRestart_NamesWhyTheStreamEnded()
        {
            Assert.Null(TranscodeTestRules.DescribeNoRestart(1, true, 40, 6.8, 13.5, 0));
            Assert.Equal("ffmpeg exited without encoding a frame.", TranscodeTestRules.DescribeNoRestart(0, false, 0.4, null, 13.5, 0));
            Assert.Equal("ffmpeg exited cleanly, but its final position or the source duration is unknown, so the end of the file could not be verified.", TranscodeTestRules.DescribeNoRestart(0, true, 1.5, 1, null, 0));
            Assert.Equal("ffmpeg exited cleanly at 6.8 s of the 13.5 s file (50%), before the end of the file.", TranscodeTestRules.DescribeNoRestart(0, true, 2, 6.75, 13.5, 0));
            Assert.Equal("The stream reached the end of the file 100 times; use a longer file.", TranscodeTestRules.DescribeNoRestart(0, true, 40, 13.5, 13.5, 100));
        }

        [Theory]
        // E2: a 13.5 s clip encoded at about 7x real time ends 2.0 s after the start.
        [InlineData(13.5, 2.0)]
        // E: a 25 s clip, about 3 s at 8x.
        [InlineData(25, 3.0)]
        public void ShortFile_EncodedFast_ToTheEnd_Restarts(double source, double lived)
        {
            var stream = NewStream(source);
            FeedFast(stream, source, lived);

            Assert.Equal(source, stream.PositionSeconds!.Value, 2);
            Assert.True(stream.AtEndOfFile);
            Assert.True(TranscodeTestRules.CanRestart(0, stream.ReportedFrames, lived, stream.AtEndOfFile, 0));
        }

        [Fact]
        public void ShortFile_CleanExitHalfway_FailsWithThePosition()
        {
            var stream = NewStream(13.5);
            FeedFast(stream, 6.75, 2.0);

            Assert.False(stream.AtEndOfFile);
            Assert.False(TranscodeTestRules.CanRestart(0, stream.ReportedFrames, 2.0, stream.AtEndOfFile, 0));
            Assert.Equal(
                "ffmpeg exited cleanly at 6.8 s of the 13.5 s file (50%), before the end of the file.",
                TranscodeTestRules.DescribeNoRestart(0, stream.ReportedFrames, 2.0, stream.PositionSeconds, stream.SourceSeconds, 0));
        }

        [Fact]
        public void FrameCount_GivesThePosition_WhenTheLineHasNoTime()
        {
            var stream = NewStream(13.5);
            stream.OnLine("frame=  324 fps=160 q=-0.0 size=N/A time=N/A bitrate=N/A speed=N/A", 2);

            // 324 frames at 24 fps.
            Assert.Equal(13.5, stream.PositionSeconds);
            Assert.True(stream.AtEndOfFile);
        }

        [Theory]
        [InlineData(2.0, false)]
        [InlineData(4.0, false)]
        [InlineData(40.0, false)]
        public void RunTimeUnknown_CannotVerifyTheEndFromLifetime(double lived, bool expected)
        {
            var stream = NewStream(null);
            FeedFast(stream, 13.5, lived);

            Assert.Null(stream.AtEndOfFile);
            Assert.Equal(expected, TranscodeTestRules.CanRestart(0, stream.ReportedFrames, lived, stream.AtEndOfFile, 0));
        }

        [Fact]
        public void NoFrames_IsNeverTheEndOfTheFile()
        {
            var stream = NewStream(13.5);
            stream.OnLine("frame=    0 fps=0.0 q=0.0 size=N/A time=N/A bitrate=N/A speed=N/A", 1);

            Assert.False(stream.ReportedFrames);
            Assert.False(TranscodeTestRules.CanRestart(0, stream.ReportedFrames, 4, stream.AtEndOfFile, 0));
            Assert.Equal("ffmpeg exited without encoding a frame.", TranscodeTestRules.DescribeNoRestart(0, stream.ReportedFrames, 4, stream.PositionSeconds, stream.SourceSeconds, 0));
        }

        [Fact]
        public void NearEnd_AndEndOfFile_ReadTheSamePosition()
        {
            // Within the end margin the stream is no longer judged, but a clean exit there is the end of the file
            // only once the position reaches the narrow EOF tolerance.
            var stream = NewStream(60);
            Feed(stream, 0, 48);

            Assert.Equal(53, stream.PositionSeconds);
            Assert.True(stream.NearEndAt(5));
            Assert.False(stream.AtEndOfFile);

            stream.OnLine("frame= 1308 fps=24 time=00:00:54.50 speed=1x", 6.5);
            Assert.Equal(54.5, stream.PositionSeconds);
            Assert.True(stream.NearEndAt(6.5));
            Assert.False(stream.AtEndOfFile);

            stream.OnLine("frame= 1428 fps=24 time=00:00:59.50 speed=1x", 11.5);
            Assert.True(stream.AtEndOfFile);
        }

        [Fact]
        public void FastStream_NearEndOfAShortFile_StillFailsWhenItStopsHalfway()
        {
            // At 7x the end margin covers the whole 13.5 s file, so the stream is not judged; that must not turn a
            // clean exit halfway into the end of the file.
            var stream = NewStream(13.5);
            FeedFast(stream, 6.75, 1.0);

            Assert.True(stream.NearEndAt(1));
            Assert.False(stream.AtEndOfFile);
        }

        [Theory]
        [InlineData(5.0, 120, 24f, 5.0)]
        // Playback's -copyts HLS command: the time lags the frames encoded at the end of the file.
        [InlineData(10.35, 321, 23.976f, 13.388)]
        // Frames dropped from a variable frame rate source: the time is further on.
        [InlineData(10.0, 200, 24f, 10.0)]
        [InlineData(null, 324, 24f, 13.5)]
        [InlineData(7.5, 180, 0f, 7.5)]
        [InlineData(null, 180, 0f, null)]
        public void ProgressPosition_TakesTheLaterOfTimeAndFrames(double? time, long frame, float fps, double? expected)
        {
            var position = TranscodeTestStream.ProgressPosition(time, frame, fps);
            Assert.Equal(expected is null, position is null);
            if (expected is double e)
            {
                Assert.Equal(e, position!.Value, 3);
            }
        }

        [Fact]
        public void ShortFile_CopytsTimeLagsTheFrames_StillReachesTheEnd()
        {
            // The last lines of the M5 run of a 13.5 s clip: every frame was encoded, the time stopped at 10.35 s.
            var stream = new TranscodeTestStream(1, new Process(), "/tmp", "p_", 23.976f, 0, 6) { SourceSeconds = 13.509 };
            stream.OnLine("[info] frame=   70 fps=0.0 q=-0.0 size=N/A time=00:00:00.00 bitrate=N/A speed=   0x elapsed=0:00:00.50", 0.5);
            stream.OnLine("[info] frame=  207 fps=206 q=-0.0 size=N/A time=00:00:05.71 bitrate=N/A speed=5.69x elapsed=0:00:01.00", 1.0);
            stream.OnLine("[info] frame=  321 fps=225 q=-0.0 Lsize=N/A time=00:00:10.35 bitrate=N/A speed=7.27x elapsed=0:00:01.42", 1.4);

            Assert.True(stream.AtEndOfFile);
            Assert.True(TranscodeTestRules.CanRestart(0, stream.ReportedFrames, 1.4, stream.AtEndOfFile, 0));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RestartedStream_ThatEnds_IsNotAStreamThatCouldNotStart(bool exited)
        {
            var kind = exited ? TranscodeTestEndKind.Exited : TranscodeTestEndKind.Stalled;
            var ended = NewStream();
            ended.OnLine("frame=  240 fps= 60 time=00:00:10.00", 4);
            var restarted = NewStream();
            restarted.CarryOver(ended);

            // The new process has no frame of its own yet, but the stream played before.
            Assert.False(restarted.ReportedFrames);
            Assert.True(restarted.EverReportedFrames);
            var (_, couldNotStart) = TranscodeTestRules.EndOutcome(kind, "h264_videotoolbox", null, null, 3, true, restarted.EverReportedFrames, 3);

            Assert.False(couldNotStart);
        }

        [Fact]
        public void NewStream_ThatEndsBeforeItsFirstFrame_CouldNotStart()
        {
            var stream = NewStream();
            var (_, couldNotStart) = TranscodeTestRules.EndOutcome(TranscodeTestEndKind.Exited, "h264_videotoolbox", null, null, 3, true, stream.EverReportedFrames, 3);

            Assert.True(couldNotStart);
        }

        [Theory]
        [InlineData("frame=  120 fps= 60 q=-0.0 size=N/A time=00:00:05.00 bitrate=N/A speed=2.5x", 5.0)]
        [InlineData("frame= 9000 fps= 60 q=-0.0 size=1kB time=01:02:03.50 bitrate=N/A speed=2.5x", 3723.5)]
        [InlineData("frame=    0 fps=0.0 q=0.0 size=N/A time=N/A bitrate=N/A speed=N/A", null)]
        public void OutputSeconds_ReadsTheTimeOfAProgressLine(string line, double? expected)
            => Assert.Equal(expected, TranscodeTestStream.OutputSeconds(line));

        [Fact]
        public void Fps_NearTheEndOfTheSource_IsNotJudged()
        {
            var stream = NewStream(70);
            Feed(stream, 0, 50);
            Assert.NotNull(stream.Fps(5));
            Assert.False(stream.NearEndAt(5));

            // Within the end margin at real time: ffmpeg is about to drain and flush.
            Feed(stream, 5, 60);
            Assert.True(stream.NearEndAt(10));
            Assert.Null(stream.Fps(10));

            // Silence must become a stop after the bounded drain allowance.
            Assert.False(stream.NearEndAt(20));
            Assert.Equal(0, stream.Fps(20));
        }

        [Fact]
        public void RunRate_ShortProcess_MeasuresFromTheFirstFrameToTheLastLine()
        {
            var stream = NewStream(13.8);
            Assert.Null(stream.RunRate);
            stream.OnLine("frame=0 fps=0 time=00:00:00.00 speed=0x", 0.5);
            stream.OnLine("frame=80 fps=160 time=00:00:03.33 speed=7x", 1);
            Assert.Null(stream.RunRate);
            stream.OnLine("frame=240 fps=160 time=00:00:10.00 speed=7x", 2);
            stream.OnLine("frame=331 fps=160 time=00:00:13.80 speed=7x", 2.5);

            // Never measured over a full window, but its whole run kept up.
            Assert.Null(stream.Fps(2.5));
            var rate = stream.RunRate;
            Assert.NotNull(rate);
            Assert.Equal((331 - 80) / 1.5, rate.Fps, 3);
            Assert.Equal(1, rate.From);
            Assert.Equal(2.5, rate.To);
        }

        [Fact]
        public void IsRestartLimit_OnlyACleanEndOfFileAtTheLimit()
        {
            Assert.True(TranscodeTestRules.IsRestartLimit(0, true, true, TranscodeTestRules.MaxRestartsPerStream));
            Assert.False(TranscodeTestRules.IsRestartLimit(0, true, true, TranscodeTestRules.MaxRestartsPerStream - 1));
            Assert.False(TranscodeTestRules.IsRestartLimit(0, true, false, TranscodeTestRules.MaxRestartsPerStream));
            Assert.False(TranscodeTestRules.IsRestartLimit(0, true, null, TranscodeTestRules.MaxRestartsPerStream));
            Assert.False(TranscodeTestRules.IsRestartLimit(1, true, true, TranscodeTestRules.MaxRestartsPerStream));
            Assert.False(TranscodeTestRules.IsRestartLimit(0, false, true, TranscodeTestRules.MaxRestartsPerStream));
            Assert.Equal(
                "Stream 2 reached the end of the file 100 times, so the test stops; 3 streams kept up before that, which is the result. A longer file gives steadier results.",
                TranscodeTestRules.DescribeRestartLimit(2, 100, 3));
        }

        [Fact]
        public void Fps_SilentFarFromTheEnd_IsStillAStop()
        {
            var stream = NewStream(600);
            Feed(stream, 0, 50);

            Assert.Equal(0, stream.Fps(20));
        }

        [Fact]
        public void Fps_WithoutARunTime_IsJudgedAsBefore()
        {
            var stream = NewStream(null);
            Feed(stream, 0, 59.5);

            Assert.False(stream.NearEndAt(5));
            Assert.NotNull(stream.Fps(5));
        }

        [Fact]
        public void Tick_AStreamWithinItsDrainAllowance_DoesNotEndTheTest()
        {
            var controller = new TranscodeTestStressController(24, 12, 5, 30, 8, 0, 3);
            for (var now = 1.0; now <= 7.5; now += 0.5)
            {
                var decision = controller.Tick(now, [new(1, 30, now, true), new(2, 10, now, true, NearEnd: true)]);
                Assert.NotEqual(TranscodeTestStressAction.LimitReached, decision.Action);
                Assert.NotEqual(TranscodeTestStressPhase.FallingBehind, decision.Phase);
            }
        }

        [Fact]
        public void Tick_AuditCounterexample_SilentNearEofStreamCannotCertifyCapacity()
        {
            var stream = NewStream(120);
            stream.OnLine("frame=2712 fps=24 time=00:01:53.00 speed=1x", 10);
            var controller = new TranscodeTestStressController(24, 12, 5, 30, 1, 0, 3);
            var draining = controller.Tick(10, [new(1, stream.Fps(10), 10, stream.ReportedFrames, stream.NearEndAt(10))]);
            Assert.Equal(TranscodeTestStressAction.Wait, draining.Action);
            Assert.Equal(0, controller.MaxStable);

            Assert.Equal(0, stream.Fps(100));
            Assert.False(stream.NearEndAt(100));
            var stopped = controller.Tick(100, [new(1, stream.Fps(100), 100, stream.ReportedFrames, stream.NearEndAt(100))]);
            Assert.Equal(TranscodeTestStressPhase.FallingBehind, stopped.Phase);
            var failed = controller.Tick(105, [new(1, stream.Fps(105), 105, stream.ReportedFrames, stream.NearEndAt(105))]);
            Assert.Equal(TranscodeTestStressAction.LimitReached, failed.Action);
            Assert.Equal(0, controller.MaxStable);
        }

        // Progress lines at 24 fps from the start of the source to the given position, spread over the given seconds.
        private static void FeedFast(TranscodeTestStream stream, double position, double seconds)
        {
            for (var i = 1; i <= 10; i++)
            {
                var p = position * i / 10;
                var frame = (long)Math.Round(p * 24);
                stream.OnLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"frame={frame} fps=160 time=00:{(int)p / 60:00}:{p % 60:00.00} speed=7x"), seconds * i / 10);
            }
        }

        // Progress lines at 24 fps of real time for 5 s from the given source position.
        private static void Feed(TranscodeTestStream stream, double startAt, double position)
        {
            for (var i = 0; i <= 10; i++)
            {
                var t = startAt + (i * 0.5);
                var p = position + (i * 0.5);
                var frame = (long)(p * 24);
                stream.OnLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"frame={frame} fps=24 time=00:{(int)p / 60:00}:{p % 60:00.00} speed=1x"), t);
            }
        }

        private static TranscodeTestStream NewStream(double? sourceSeconds = null)
            => new(1, new Process(), "/tmp", "p_", 24f, 0, 6) { SourceSeconds = sourceSeconds };
    }
}
