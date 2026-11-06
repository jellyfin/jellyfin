using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// How a stream started again at the end of the file keeps its output, and how the result averages it.
    /// </summary>
    public sealed class TranscodeTestStressRunTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "transcode-test-run-" + Guid.NewGuid().ToString("N"));

        public TranscodeTestStressRunTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose() => Directory.Delete(_folder, true);

        [Fact]
        public void CollectSegments_AfterExit_CountsTheNewestSegmentToo()
        {
            var stream = Stream(1, "a_", 3, 750_000);

            stream.CollectSegments();
            Assert.Equal(1.0, stream.OutputMbps);

            stream.CollectSegments(true);
            Assert.Equal(1.0, stream.OutputMbps);
            Assert.Equal(3, Directory.GetFiles(_folder, "a_*").Length);
        }

        [Fact]
        public void CarryOver_RestartedStream_KeepsItsOutputMbps()
        {
            var ended = Stream(1, "a_", 3, 750_000);
            ended.CollectSegments(true);
            var restarted = Stream(1, "b_", 0, 0);

            restarted.CarryOver(ended);

            Assert.Equal(1.0, restarted.OutputMbps);

            // Its own segments add to the ones before.
            WriteSegments("b_", 3, 1_500_000);
            restarted.CollectSegments(true);
            Assert.Equal(1.5, restarted.OutputMbps);
        }

        [Fact]
        public void Snapshot_AverageOutputMbps_UsesTheShownStreams()
        {
            var run = new TranscodeTestStressRun(8);
            var ended = Stream(1, "a_", 3, 750_000);
            ended.CollectSegments(true);
            var restarted = Stream(1, "b_", 0, 0);
            restarted.CarryOver(ended);
            var second = Stream(2, "c_", 3, 1_500_000);
            second.CollectSegments(true);
            run.Add(ended);
            run.Add(restarted);
            run.Add(second);

            var result = run.Snapshot(10);

            // Stream 1 is shown once, as its newest process, with the output of both.
            Assert.Equal(2, result.Streams.Count);
            Assert.Equal(1.0, result.Streams[0].OutputMbps);
            Assert.Equal(2.0, result.Streams[1].OutputMbps);
            Assert.Equal(1.5, result.AverageOutputMbps);
        }

        [Fact]
        public void Snapshot_ReportsTheRampAndSettleWindowsOfTheRule()
        {
            var result = new TranscodeTestStressRun(8).Snapshot(0);

            // The dashboard words the rule from these, as DescribeRamp does in the log.
            Assert.Equal(TranscodeTestManager.RampSeconds, result.RampSeconds);
            Assert.Equal(TranscodeTestStream.WindowSeconds, result.SettleSeconds);
        }

        [Fact]
        public void SingleStream_AfterARestart_KeepsTheFirstProcessesMeasurement()
        {
            var run = new TranscodeTestStressRun(8);
            var ended = Stream(1, "a_", 0, 0);
            for (var t = 1.0; t <= 10; t += 0.5)
            {
                ended.OnLine(FormattableString.Invariant($"frame= {(int)(t * 30)} fps= 30"), t);
                if (t % 1 == 0)
                {
                    ended.Sample(t);
                }
            }

            var restarted = Stream(1, "b_", 0, 0);
            restarted.CarryOver(ended);
            run.Add(ended);
            run.Add(restarted);

            // The new process has no reading of its own yet.
            Assert.Null(restarted.Fps(11));
            run.RecordSingleStream(11);

            Assert.Equal(30, run.Snapshot(11).SingleStreamFps);
            Assert.Equal(30, run.Snapshot(11).Streams[0].MedianFps);
        }

        [Fact]
        public void Snapshot_AStreamThatEndedWithoutFrames_DidNotStart()
        {
            var run = new TranscodeTestStressRun(8);
            var started = Stream(1, "a_", 0, 0);
            started.OnLine("frame=   10 fps= 24", 2);
            var stalled = Stream(2, "b_", 0, 0);
            run.Add(started);
            run.Add(stalled);

            Assert.Equal([TranscodeTestStreamStatus.Running, TranscodeTestStreamStatus.Running], run.Snapshot(5).Streams.Select(s => s.Status));

            run.End(TranscodeTestEndReason.StreamStalled, 35, 2, waitingOnInput: true);
            var result = run.Snapshot(35);

            Assert.Equal([TranscodeTestStreamStatus.Running, TranscodeTestStreamStatus.NotStarted], result.Streams.Select(s => s.Status));
            Assert.True(result.WaitingOnInput);
        }

        [Theory]
        [InlineData(TranscodeTestEndReason.LimitReached, true)]
        [InlineData(TranscodeTestEndReason.Cancelled, false)] // Only a limit is described as a collapse.
        public void End_StreamsThatAllWentQuiet_Collapsed(TranscodeTestEndReason reason, bool collapsed)
        {
            var run = new TranscodeTestStressRun(8) { ThresholdFps = 24 };
            foreach (var id in new[] { 1, 2 })
            {
                var stream = Stream(id, id + "_", 0, 0);
                stream.OnLine("frame=   10 fps= 24", 2);
                run.Add(stream);
            }

            // Both reported frames and then went quiet, so both read 0 fps.
            run.End(reason, 20, 2, 0);

            Assert.Equal(collapsed, run.Snapshot(20).Collapsed);
            Assert.Equal(reason, run.Snapshot(20).EndReason);
        }

        [Fact]
        public void Snapshot_ARestartedStreamThatProducedFramesBefore_Started()
        {
            var run = new TranscodeTestStressRun(8);
            var ended = Stream(1, "a_", 0, 0);
            ended.OnLine("frame=   10 fps= 24", 2);
            var restarted = Stream(1, "b_", 0, 0);
            restarted.CarryOver(ended);
            run.Add(ended);
            run.Add(restarted);

            run.End(TranscodeTestEndReason.Cancelled, 10);

            Assert.Equal(TranscodeTestStreamStatus.Running, run.Snapshot(10).Streams[0].Status);
            Assert.False(run.Snapshot(10).WaitingOnInput);
        }

        // A running process, so the stream reads as running.
        private TranscodeTestStream Stream(int id, string prefix, int segments, int bytes)
        {
            WriteSegments(prefix, segments, bytes);
            return new TranscodeTestStream(id, Process.GetCurrentProcess(), _folder, prefix, 24f, 0, 6);
        }

        private void WriteSegments(string prefix, int count, int bytes)
        {
            for (var i = 0; i < count; i++)
            {
                File.WriteAllBytes(Path.Combine(_folder, prefix + i.ToString(CultureInfo.InvariantCulture) + ".ts"), new byte[bytes]);
            }
        }
    }
}
