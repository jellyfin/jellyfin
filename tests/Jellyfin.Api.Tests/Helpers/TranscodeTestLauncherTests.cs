using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// A blocked process launch must time out or cancel without blocking the test, then clean up any late result.
    /// </summary>
    public sealed class TranscodeTestLauncherTests : IDisposable
    {
        private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);
        private readonly ManualResetEventSlim _release = new();

        public void Dispose()
        {
            _release.Set();
            _release.Dispose();
        }

        [Fact]
        public async Task StartAsync_ALaunchThatReturns_GivesTheProcess()
        {
            var started = new object();
            var abandoned = false;
            var result = await TranscodeTestLauncher.StartAsync(() => started, _wait, _ => abandoned = true, TestContext.Current.CancellationToken);
            Assert.Same(started, result);
            Assert.False(abandoned);
        }

        [Fact]
        public async Task StartAsync_DoesNotBlockTheCallerWhileTheLaunchRuns()
        {
            var started = new object();
            var launch = TranscodeTestLauncher.StartAsync(Hang(started), _wait, _ => { }, TestContext.Current.CancellationToken);
            Assert.False(launch.IsCompleted);
            _release.Set();
            Assert.Same(started, await launch.WaitAsync(_wait, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task StartAsync_ALaunchThatHangs_TimesOutAndStopsTheProcessWhenItStartsLater()
        {
            var late = new object();
            var abandoned = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var clock = Stopwatch.StartNew();
            var result = await TranscodeTestLauncher.StartAsync(Hang(late), TimeSpan.FromMilliseconds(200), abandoned.SetResult, TestContext.Current.CancellationToken);
            Assert.Null(result);
            Assert.True(clock.Elapsed < _wait);
            Assert.False(abandoned.Task.IsCompleted);
            _release.Set();
            Assert.Same(late, await abandoned.Task.WaitAsync(_wait, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task StartAsync_CancelledWhileTheLaunchHangs_ThrowsAndStopsTheProcessWhenItStartsLater()
        {
            var late = new object();
            var abandoned = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var launch = TranscodeTestLauncher.StartAsync(Hang(late), _wait, abandoned.SetResult, cancellation.Token);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => launch.WaitAsync(_wait, TestContext.Current.CancellationToken));
            _release.Set();
            Assert.Same(late, await abandoned.Task.WaitAsync(_wait, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task StartAsync_ALaunchThatFails_PassesOnTheError()
        {
            var abandoned = false;
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => TranscodeTestLauncher.StartAsync<object>(() => throw new InvalidOperationException("no such file"), _wait, _ => abandoned = true, TestContext.Current.CancellationToken));
            Assert.False(abandoned);
        }

        [Fact]
        public async Task StartAsync_ALaunchThatFailsAfterTheTimeout_IsNotAbandoned()
        {
            var abandoned = false;
            var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = await TranscodeTestLauncher.StartAsync<object>(
                () =>
                {
                    _release.Wait(_wait);
                    failed.SetResult();
                    throw new InvalidOperationException("no such file");
                },
                TimeSpan.FromMilliseconds(100),
                _ => abandoned = true,
                TestContext.Current.CancellationToken);
            Assert.Null(result);
            _release.Set();
            await failed.Task.WaitAsync(_wait, TestContext.Current.CancellationToken);
            Assert.False(abandoned);
        }

        [Fact]
        public void DescribeLaunchTimeout_NamesTheTimeoutAndTheLikelyCause()
            => Assert.Equal(
                "The system did not run ffmpeg within 30 s; check that the storage holding ffmpeg responds.",
                TranscodeTestRules.DescribeLaunchTimeout(TranscodeTestManager.StartTimeoutSeconds));

        private Func<object> Hang(object result)
            => () =>
            {
                _release.Wait(_wait);
                return result;
            };
    }
}
