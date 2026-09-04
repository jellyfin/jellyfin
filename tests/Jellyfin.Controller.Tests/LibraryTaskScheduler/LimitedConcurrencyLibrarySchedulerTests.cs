using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.LibraryTaskScheduler;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.LibraryTaskScheduler
{
    public class LimitedConcurrencyLibrarySchedulerTests
    {
        private static readonly TimeSpan _shortGracePeriod = TimeSpan.FromMilliseconds(50);

        // Generous, because these only ever wait for something that should already have happened.
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        [Fact]
        public async Task Enqueue_ProcessesEveryItem()
        {
            using var appStopping = new CancellationTokenSource();
            var scheduler = CreateScheduler(appStopping);
            await using (scheduler)
            {
                var data = Enumerable.Range(0, 100).ToArray();
                var processed = new ConcurrentBag<int>();

                await scheduler.Enqueue(
                    data,
                    (item, _) =>
                    {
                        processed.Add(item);
                        return Task.CompletedTask;
                    },
                    new Progress<double>(),
                    CancellationToken.None);

                Assert.Equal(data, processed.Order());
            }
        }

        [Fact]
        public async Task Enqueue_WithFailingWorker_StillCompletes()
        {
            using var appStopping = new CancellationTokenSource();
            var scheduler = CreateScheduler(appStopping);
            await using (scheduler)
            {
                await scheduler.Enqueue(
                    Enumerable.Range(0, 20).ToArray(),
                    (item, _) => item % 2 == 0 ? throw new InvalidOperationException("boom") : Task.CompletedTask,
                    new Progress<double>(),
                    CancellationToken.None);
            }
        }

        /// <summary>
        /// The runners wait on a source linked to <see cref="IHostApplicationLifetime.ApplicationStopping"/>,
        /// so a shutdown has to reach them. It does not travel from the linked source back to the one
        /// the cleanup cancels, which is what made them immortal.
        /// </summary>
        [Fact]
        public async Task ApplicationStopping_RetiresRunners()
        {
            using var appStopping = new CancellationTokenSource();

            // Long enough that the cleanup cannot be what retires them.
            var scheduler = CreateScheduler(appStopping, gracePeriod: TimeSpan.FromMinutes(5));
            await using (scheduler)
            {
                await RunOneOperation(scheduler);
                Assert.True(scheduler.ActiveRunnerCount > 0);

                await appStopping.CancelAsync();

                await WaitForAsync(() => scheduler.ActiveRunnerCount == 0);
            }
        }

        /// <summary>
        /// The cleanup used to be a one shot: it never released the scheduling slot it took, so
        /// every runner spawned after the first pass stayed around for the lifetime of the server.
        /// </summary>
        [Fact]
        public async Task Enqueue_RetiresIdleRunnersAfterEveryOperation()
        {
            using var appStopping = new CancellationTokenSource();
            var scheduler = CreateScheduler(appStopping);
            await using (scheduler)
            {
                for (var round = 0; round < 3; round++)
                {
                    await RunOneOperation(scheduler);
                    Assert.True(scheduler.ActiveRunnerCount > 0, $"no runner spawned in round {round}");

                    await WaitForAsync(() => scheduler.ActiveRunnerCount == 0);
                }
            }
        }

        /// <summary>
        /// Disposing used to sit out the rest of the cleanup grace period, holding up shutdown for
        /// up to a minute.
        /// </summary>
        [Fact]
        public async Task DisposeAsync_DoesNotWaitOutTheGracePeriod()
        {
            using var appStopping = new CancellationTokenSource();
            var scheduler = CreateScheduler(appStopping, gracePeriod: TimeSpan.FromMinutes(5));

            await RunOneOperation(scheduler);

            var stopwatch = Stopwatch.StartNew();
            await scheduler.DisposeAsync();

            Assert.True(stopwatch.Elapsed < _timeout, $"disposing took {stopwatch.Elapsed}");
        }

        [Fact]
        public async Task Enqueue_AfterDispose_DoesNothing()
        {
            using var appStopping = new CancellationTokenSource();
            var scheduler = CreateScheduler(appStopping);
            await scheduler.DisposeAsync();

            var processed = 0;
            await scheduler.Enqueue(
                Enumerable.Range(0, 10).ToArray(),
                (_, _) =>
                {
                    Interlocked.Increment(ref processed);
                    return Task.CompletedTask;
                },
                new Progress<double>(),
                CancellationToken.None);

            Assert.Equal(0, processed);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        public async Task Enqueue_FromWithinAWorker_DoesNotDeadlock(int fanout)
        {
            using var appStopping = new CancellationTokenSource();
            var scheduler = CreateScheduler(appStopping, fanout: fanout);
            await using (scheduler)
            {
                var inner = 0;

                var outer = scheduler.Enqueue(
                    Enumerable.Range(0, 8).ToArray(),
                    (_, _) => scheduler.Enqueue(
                        Enumerable.Range(0, 4).ToArray(),
                        (_, _) =>
                        {
                            Interlocked.Increment(ref inner);
                            return Task.CompletedTask;
                        },
                        new Progress<double>(),
                        CancellationToken.None),
                    new Progress<double>(),
                    CancellationToken.None);

                await outer.WaitAsync(_timeout, TestContext.Current.CancellationToken);

                Assert.Equal(32, inner);
            }
        }

        private static LimitedConcurrencyLibraryScheduler CreateScheduler(
            CancellationTokenSource appStopping,
            int fanout = 4,
            TimeSpan? gracePeriod = null)
        {
            var lifetime = new Mock<IHostApplicationLifetime>();
            lifetime.SetupGet(x => x.ApplicationStopping).Returns(() => appStopping.Token);

            var configurationManager = new Mock<IServerConfigurationManager>();
            configurationManager.SetupGet(x => x.Configuration)
                .Returns(new ServerConfiguration { LibraryScanFanoutConcurrency = fanout });

            return new LimitedConcurrencyLibraryScheduler(
                lifetime.Object,
                NullLogger<LimitedConcurrencyLibraryScheduler>.Instance,
                configurationManager.Object,
                gracePeriod ?? _shortGracePeriod);
        }

        private static Task RunOneOperation(LimitedConcurrencyLibraryScheduler scheduler)
            => scheduler.Enqueue(
                Enumerable.Range(0, 8).ToArray(),
                (_, _) => Task.CompletedTask,
                new Progress<double>(),
                CancellationToken.None);

        private static async Task WaitForAsync(Func<bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(stopwatch.Elapsed < _timeout, "timed out waiting for the scheduler to settle");
                await Task.Delay(20, TestContext.Current.CancellationToken);
            }
        }
    }
}
