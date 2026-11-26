using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// How the stress test's ffmpeg processes are stopped when the server shuts down, and how a stream that exited is
    /// read safely from any thread.
    /// </summary>
    public sealed class TranscodeTestShutdownTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "transcode-test-shutdown-" + Guid.NewGuid().ToString("N"));

        public TranscodeTestShutdownTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose() => Directory.Delete(_folder, true);

        [Fact]
        public void StopNow_KillsEveryProcessAndRemovesTheTestsFiles()
        {
            var run = new TranscodeTestStressRun(8) { Folder = _folder, FilePrefix = "transcode-test-1" };
            var streams = new[] { Stream(1, LongRunning()), Stream(2, LongRunning()) };
            foreach (var s in streams)
            {
                Assert.True(run.Add(s));
            }

            File.WriteAllBytes(Path.Combine(_folder, "transcode-test-1-s1-0_0.ts"), new byte[10]);
            File.WriteAllBytes(Path.Combine(_folder, "transcode-test-1-s2-1_.m3u8"), new byte[10]);
            File.WriteAllBytes(Path.Combine(_folder, "other.ts"), new byte[10]);

            TranscodeTestManager.StopNow(run, TimeSpan.FromSeconds(5), NullLogger.Instance);

            Assert.All(streams, s => Assert.True(s.HasExited));
            Assert.All(streams, s => Assert.True(s.Stopping));
            Assert.Equal(["other.ts"], Directory.GetFiles(_folder).Select(Path.GetFileName));
        }

        [Fact]
        public void StopNow_DoesNotWaitPerStreamForAnInheritedOutputPipe()
        {
            var run = new TranscodeTestStressRun(3);
            var reader = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                for (var id = 1; id <= 3; id++)
                {
                    var stream = Stream(id, Exiting(0));
                    Assert.True(stream.WaitForExit(TimeSpan.FromSeconds(15)));
                    // Models a reader whose pipe stays open after the actual process has exited.
                    stream.StartReading(() => reader.Task);
                    Assert.True(run.Add(stream));
                }

                var elapsed = Stopwatch.StartNew();
                TranscodeTestManager.StopNow(run, TimeSpan.FromMilliseconds(100), NullLogger.Instance);

                Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(1), $"Shutdown added per-reader waits: {elapsed.Elapsed}.");
                Assert.All(run.GetStreams(), stream => Assert.True(stream.HasExited));
            }
            finally
            {
                reader.TrySetResult();
                TranscodeTestManager.StopNow(run, TimeSpan.Zero, NullLogger.Instance);
            }
        }

        [Fact]
        public async Task StopNow_IsSafeTwiceAndWhileTheTestStopsTheStreamsToo()
        {
            var run = new TranscodeTestStressRun(8) { Folder = _folder, FilePrefix = "transcode-test-2" };
            var stream = Stream(1, LongRunning());
            run.Add(stream);

            var other = Task.Run(() => TranscodeTestManager.StopNow(run, TimeSpan.FromSeconds(5), NullLogger.Instance), TestContext.Current.CancellationToken);
            TranscodeTestManager.StopNow(run, TimeSpan.FromSeconds(5), NullLogger.Instance);
            await other;
            TranscodeTestManager.StopNow(run, TimeSpan.FromSeconds(5), NullLogger.Instance);

            Assert.True(stream.HasExited);
        }

        [Fact]
        public void Add_AfterStop_RefusesTheStream()
        {
            var run = new TranscodeTestStressRun(8);
            run.Stop();

            // The caller then kills the process it just started, so a stream started while the server stops never runs on.
            Assert.False(run.Add(Stream(1, Exiting(0))));
            Assert.Empty(run.Streams);
        }

        [Fact]
        public void StopNow_WithoutTheFolder_DoesNotThrow()
        {
            var run = new TranscodeTestStressRun(8) { Folder = Path.Combine(_folder, "missing"), FilePrefix = "transcode-test-3" };
            TranscodeTestManager.StopNow(run, TimeSpan.FromSeconds(1), NullLogger.Instance);
        }

        [Fact]
        public void DisposedStream_KeepsItsExitCodeAndTheRunCanStillBeRead()
        {
            var process = Exiting(3);
            var stream = Stream(1, process);
            Assert.True(stream.WaitForExit(TimeSpan.FromSeconds(15)));
            var run = new TranscodeTestStressRun(8);
            run.Add(stream);

            stream.DisposeProcess();
            stream.DisposeProcess();

            Assert.True(stream.HasExited);
            Assert.Equal(3, stream.ExitCode);
            Assert.Equal(TranscodeTestStreamStatus.Failed, run.Snapshot(1).Streams[0].Status);
        }

        [Fact]
        public async Task WaitForOutputAsync_ReadsTheLastLinesOfAnExitedStream()
        {
            var stream = Stream(1, Printing("[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: incompatible client key (21)", 1));
            stream.StartReading(Stopwatch.StartNew());
            Assert.True(stream.WaitForExit(TimeSpan.FromSeconds(15)));

            await stream.WaitForOutputAsync(TimeSpan.FromSeconds(10));

            Assert.Contains("incompatible client key (21)", stream.LastError, StringComparison.Ordinal);
            Assert.Equal(1, stream.ExitCode);
        }

        [Theory]
        [InlineData("[matroska,webm @ 0x0] [warning] File ended prematurely", 120, false, true)]
        [InlineData("[vist#0:0/h264] [verbose] Decoder returned EOF, finishing", 72, true, false)]
        public async Task OutputReader_ReadsTheFinalDiagnosticWithoutANewline(string diagnostic, int frames, bool expectedEof, bool incompleteInput)
        {
            var stream = new TranscodeTestStream(1, PrintingWithoutNewline(diagnostic), _folder, "transcode-test-final_", 24f, 0, 6)
            {
                SourceSeconds = 5
            };
            try
            {
                stream.OnLine("frame=" + frames + " fps=24", 0);
                stream.StartReading(Stopwatch.StartNew());
                Assert.True(stream.WaitForExit(TimeSpan.FromSeconds(15)));
                await stream.WaitForOutputAsync(TimeSpan.FromSeconds(10));

                Assert.True(stream.OutputSucceeded);
                Assert.Equal(expectedEof, stream.AtEndOfFile);
                Assert.Equal(incompleteInput, stream.PrematureEndLine is not null);
            }
            finally
            {
                stream.DisposeProcess(false);
            }
        }

        [Fact]
        public async Task DisposeProcess_WhileTheOutputIsRead_EndsTheReaderQuietly()
        {
            var stream = Stream(1, LongRunning());
            stream.StartReading(Stopwatch.StartNew());

            stream.Kill();
            stream.DisposeProcess();
            await stream.WaitForOutputAsync(TimeSpan.FromSeconds(10));

            Assert.True(stream.HasExited);
        }

        private TranscodeTestStream Stream(int id, Process process)
            => new(id, process, _folder, "transcode-test-x-s" + id + "_", 24f, 0, 6);

        private static Process LongRunning()
            => Start(OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh", OperatingSystem.IsWindows() ? "/c ping -n 60 127.0.0.1 >nul" : "-c \"echo started >&2; sleep 60\"");

        private static Process Exiting(int code)
            => Start(OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh", OperatingSystem.IsWindows() ? "/c exit " + code : "-c \"exit " + code + "\"");

        private static Process Printing(string line, int code)
            => Start(OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh", OperatingSystem.IsWindows() ? "/c echo " + line + " 1>&2 & exit " + code : "-c \"echo '" + line + "' >&2; exit " + code + "\"");

        private static Process PrintingWithoutNewline(string line)
            => Start(OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh", OperatingSystem.IsWindows() ? "/c <nul set /p \"=" + line + "\" 1>&2 & exit 0" : "-c \"printf '%s' '" + line + "' >&2; exit 0\"");

        private static Process Start(string file, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(file, arguments)
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            return process;
        }
    }
}
