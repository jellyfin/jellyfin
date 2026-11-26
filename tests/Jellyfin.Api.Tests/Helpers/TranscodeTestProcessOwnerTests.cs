using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public sealed class TranscodeTestProcessOwnerTests
    {
        [Fact]
        public void Dispose_KillsAllCompatibilityProcessesBeforeReturning()
        {
            using var owner = new TranscodeTestProcessOwner();
            using var first = LongRunning();
            using var second = LongRunning();
            try
            {
                Assert.True(owner.TryAdd(first));
                Assert.True(owner.TryAdd(second));
                owner.Dispose();
                Assert.True(first.HasExited);
                Assert.True(second.HasExited);
                Assert.False(owner.TryAdd(first));
                owner.Dispose();
            }
            finally
            {
                TranscodeTestProcessOwner.Kill(first);
                TranscodeTestProcessOwner.Kill(second);
            }
        }

        [Fact]
        public async Task Dispose_DoesNotRaceTheCallersOutputReader()
        {
            using var owner = new TranscodeTestProcessOwner();
            using var process = LongRunning();
            try
            {
                Assert.True(owner.TryAdd(process));
                var output = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
                owner.Dispose();
                await output.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.True(process.HasExited);
                owner.Remove(process);
            }
            finally
            {
                TranscodeTestProcessOwner.Kill(process);
            }
        }

        [Fact]
        public void TryAdd_AfterShutdownRejectsALateLaunch()
        {
            using var owner = new TranscodeTestProcessOwner();
            owner.Dispose();
            using var late = LongRunning();
            try
            {
                Assert.False(owner.TryAdd(late));
                TranscodeTestProcessOwner.Kill(late);
                TranscodeTestProcessOwner.WaitForExit(late, TimeSpan.FromSeconds(5));
                Assert.True(late.HasExited);
            }
            finally
            {
                TranscodeTestProcessOwner.Kill(late);
            }
        }

        [Fact]
        public void Manager_DisposedInstanceRejectsStartsAndAllowsRepeatedCancelAndDispose()
        {
            using var manager = new TranscodeTestManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, NullLogger<TranscodeTestManager>.Instance);
            manager.Dispose();
            Assert.False(manager.TryStart(new() { Kind = Jellyfin.Api.Models.TranscodeTestDtos.TranscodeTestKind.Compatibility }));
            manager.Cancel();
            manager.Dispose();
            Assert.Equal(Jellyfin.Api.Models.TranscodeTestDtos.TranscodeTestState.Idle, manager.GetStatus().State);
        }

        private static Process LongRunning()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(
                    OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                    OperatingSystem.IsWindows() ? "/c ping -n 60 127.0.0.1 >nul" : "-c \"echo started >&2; sleep 60\"")
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
