using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.MediaEncoding.Transcoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Transcoding;

public sealed class TranscodeManagerTests
{
    [Fact]
    public async Task OnTranscodeEndRequest_UsesActiveJobsLock()
    {
        using var manager = CreateManager();
        using var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
        {
            ActiveRequestCount = 2
        };

        var field = typeof(TranscodeManager).GetField("_activeTranscodingJobs", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var activeJobs = Assert.IsType<List<TranscodingJob>>(field.GetValue(manager));
        var cancellationToken = TestContext.Current.CancellationToken;
        using var releaseLock = new ManualResetEventSlim();
        var lockAcquired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lockHolder = Task.Run(
            () =>
            {
                lock (activeJobs)
                {
                    lockAcquired.TrySetResult(true);
                    releaseLock.Wait(cancellationToken);
                }
            },
            cancellationToken);
        await lockAcquired.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        var decrementStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var decrementTask = Task.Run(
            () =>
            {
                decrementStarted.TrySetResult(true);
                manager.OnTranscodeEndRequest(job);
            },
            cancellationToken);
        await decrementStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            Assert.False(decrementTask.IsCompleted);
        }
        finally
        {
            releaseLock.Set();
            await Task.WhenAll(lockHolder, decrementTask);
        }

        Assert.Equal(1, job.ActiveRequestCount);
    }

    [Fact]
    public void ReserveTranscodingSlot_WhenLimitReached_ThrowsExplicitRateLimit()
    {
        using var manager = CreateManager(maximumTranscodingJobs: 1);
        using var firstReservation = manager.ReserveTranscodingSlot("first");

        var exception = Assert.Throws<RateLimitExceededException>(
            () => manager.ReserveTranscodingSlot("second"));

        Assert.Contains("maximum of 1 concurrent FFmpeg jobs", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReserveTranscodingSlot_WhenReservationDisposed_AllowsAnotherJob()
    {
        using var manager = CreateManager(maximumTranscodingJobs: 1);
        var firstReservation = manager.ReserveTranscodingSlot("first");

        firstReservation.Dispose();
        firstReservation.Dispose();

        using var secondReservation = manager.ReserveTranscodingSlot("second");
    }

    private static TranscodeManager CreateManager(int maximumTranscodingJobs = 2)
    {
        var applicationPaths = Mock.Of<IApplicationPaths>();
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                MaxConcurrentTranscodingJobs = maximumTranscodingJobs
            });
        configurationManager.Setup(manager => manager.GetConfiguration("encoding"))
            .Returns(new EncodingOptions
            {
                TranscodingTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            });
        configurationManager.SetupGet(manager => manager.CommonApplicationPaths).Returns(applicationPaths);

        return new TranscodeManager(
            NullLoggerFactory.Instance,
            Mock.Of<IFileSystem>(),
            applicationPaths,
            configurationManager.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<ISessionManager>(),
            null!,
            Mock.Of<IMediaEncoder>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IAttachmentExtractor>());
    }
}
