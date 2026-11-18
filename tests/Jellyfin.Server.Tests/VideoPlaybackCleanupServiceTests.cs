using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.HostedServices;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests;

public class VideoPlaybackCleanupServiceTests
{
    [Fact]
    public async Task SourceCleanupFailure_IsLoggedAndNextTickStillRuns()
    {
        var coordinator = new Mock<IVideoPlaybackCoordinator>();
        var sources = new Mock<ILiveStreamPlaybackManager>();
        sources.Setup(source => source.GetProtectedPlaySessions()).Returns(new HashSet<string>());
        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        sources.Setup(source => source.RemoveExpiredAsync(It.IsAny<IReadOnlyCollection<TranscodingJob>>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    throw new IOException("Provider close failed.");
                }

                recovered.TrySetResult();
                return Task.CompletedTask;
            });
        var clock = new ManualTimerProvider();
        var logger = new ErrorLogger();
        using var service = new VideoPlaybackCleanupService(coordinator.Object, null, sources.Object, logger, clock);
        var cancellation = TestContext.Current.CancellationToken;
        await service.StartAsync(cancellation);
        var timer = await clock.Ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellation);

        timer.Tick();
        await logger.Error.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellation);
        Assert.False(service.ExecuteTask!.IsCompleted);
        timer.Tick();
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellation);
        await service.StopAsync(cancellation);

        Assert.Equal(2, attempts);
        Assert.False(service.ExecuteTask.IsFaulted);
        coordinator.Verify(registry => registry.RemoveExpired(It.IsAny<IReadOnlySet<string>>()), Times.Exactly(2));
    }

    private sealed class ErrorLogger : ILogger<VideoPlaybackCleanupService>
    {
        public TaskCompletionSource Error { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                Error.TrySetResult();
            }
        }
    }

    private sealed class ManualTimerProvider : TimeProvider
    {
        public TaskCompletionSource<ManualTimer> Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state);
            Ready.TrySetResult(timer);
            return timer;
        }
    }

    private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
    {
        private bool _disposed;

        public bool Change(TimeSpan dueTime, TimeSpan period) => !_disposed;

        public void Tick()
        {
            if (!_disposed)
            {
                callback(state);
            }
        }

        public void Dispose() => _disposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
