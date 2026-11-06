using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Jellyfin.Api.Helpers;
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
