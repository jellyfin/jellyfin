using System;
using System.Threading.Tasks;
using Emby.Server.Implementations.EntryPoints;
using Emby.Server.Implementations.Localization;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EntryPoints;

// The host subscribes to the static CollectionFolder.LibraryOptionsUpdated, so tests must not run in parallel.
[Collection(ScheduledTasks.BaseItemStaticsCollection.Name)]
public sealed class RatingsRecomputeHostTests : IDisposable
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    private readonly ServerConfiguration _serverConfiguration = new() { MetadataCountryCode = "US" };
    private readonly Mock<IServerConfigurationManager> _configurationManager = new();
    private readonly Mock<IServerApplicationHost> _applicationHost = new();
    private readonly Mock<ITaskManager> _taskManager = new();
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly RatingsStateStore _store;
    private readonly RatingsRecomputeHost _host;

    private RatingsStateData _stored = new();

    public RatingsRecomputeHostTests()
    {
        _configurationManager.SetupGet(x => x.Configuration).Returns(_serverConfiguration);
        _configurationManager.Setup(x => x.GetConfiguration(RatingsStateConfigurationFactory.Key)).Returns(() => _stored);
        _configurationManager
            .Setup(x => x.SaveConfiguration(RatingsStateConfigurationFactory.Key, It.IsAny<object>()))
            .Callback<string, object>((_, data) => _stored = (RatingsStateData)data);
        _libraryManager.Setup(x => x.GetVirtualFolders()).Returns([]);
        _applicationHost.SetupGet(x => x.CoreStartupHasCompleted).Returns(true);

        _store = new RatingsStateStore(_configurationManager.Object, _libraryManager.Object, () => "v1");
        _host = new RatingsRecomputeHost(
            _configurationManager.Object,
            _applicationHost.Object,
            _taskManager.Object,
            _store,
            NullLogger<RatingsRecomputeHost>.Instance)
        {
            CoalesceDelay = TimeSpan.FromMilliseconds(50)
        };
    }

    public void Dispose() => _host.Dispose();

    private void VerifyQueued(Times times)
        => _taskManager.Verify(x => x.QueueIfNotRunning<RecomputeParentalRatingScoresTask>(), times);

    private async Task StartAndWaitForStartupCheck()
    {
        await _host.StartAsync(TestContext.Current.CancellationToken);
        await _host.StartupCheck!.WaitAsync(_timeout, TestContext.Current.CancellationToken);
    }

    private Task NextEvaluation() => _host.WaitForNextEvaluationAsync().WaitAsync(_timeout, TestContext.Current.CancellationToken);

    private static RecomputeParentalRatingScoresTask CreateTask(MediaBrowser.Model.Globalization.ILocalizationManager localization)
        => new(
            Mock.Of<ILibraryManager>(),
            Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<Jellyfin.Database.Implementations.JellyfinDbContext>>(),
            null!,
            localization,
            NullLogger<RecomputeParentalRatingScoresTask>.Instance);

    private void RaiseTaskCompleted(TaskCompletionStatus status, bool recomputeTask = true)
    {
        IScheduledTask task = recomputeTask ? CreateTask(Mock.Of<MediaBrowser.Model.Globalization.ILocalizationManager>()) : Mock.Of<IScheduledTask>();
        var worker = new Mock<IScheduledTaskWorker>();
        worker.SetupGet(x => x.ScheduledTask).Returns(task);
        _taskManager.Raise(x => x.TaskCompleted += null, new TaskCompletionEventArgs(worker.Object, new TaskResult { Status = status }));
    }

    [Fact]
    public async Task Startup_NoStoredState_QueuesTask()
    {
        await StartAndWaitForStartupCheck();

        VerifyQueued(Times.Once());
    }

    [Fact]
    public async Task Startup_StoredStateMatches_EvaluatesAndDoesNotQueueTask()
    {
        _store.Save(_store.ComputeCurrent());

        await StartAndWaitForStartupCheck();

        Assert.Equal(1, _host.EvaluationCount);
        VerifyQueued(Times.Never());
    }

    [Fact]
    public async Task Startup_RatingsVersionDiffers_QueuesTask()
    {
        var version = "v0";
        var store = new RatingsStateStore(_configurationManager.Object, _libraryManager.Object, () => version);
        store.Save(store.ComputeCurrent());
        version = "v1";
        using var host = new RatingsRecomputeHost(_configurationManager.Object, _applicationHost.Object, _taskManager.Object, store, NullLogger<RatingsRecomputeHost>.Instance);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StartupCheck!.WaitAsync(_timeout, TestContext.Current.CancellationToken);

        VerifyQueued(Times.Once());
    }

    [Fact]
    public async Task Startup_WaitsUntilCoreStartupCompleted()
    {
        var completed = false;
        _applicationHost.SetupGet(x => x.CoreStartupHasCompleted).Returns(() => completed);

        await _host.StartAsync(TestContext.Current.CancellationToken);
        Assert.False(_host.StartupCheck!.IsCompleted);
        Assert.Equal(0, _host.EvaluationCount);

        completed = true;
        await _host.StartupCheck.WaitAsync(_timeout, TestContext.Current.CancellationToken);

        VerifyQueued(Times.Once());
    }

    [Fact]
    public async Task ConfigurationUpdated_ServerCountryChanged_QueuesTaskOnceForABurst()
    {
        _store.Save(_store.ComputeCurrent());
        await StartAndWaitForStartupCheck();
        var evaluation = NextEvaluation();

        _serverConfiguration.MetadataCountryCode = "DE";
        for (var i = 0; i < 5; i++)
        {
            _configurationManager.Raise(x => x.ConfigurationUpdated += null, EventArgs.Empty);
        }

        Assert.True(_host.IsEvaluationScheduled);
        await evaluation;

        Assert.Equal(2, _host.EvaluationCount);
        VerifyQueued(Times.Once());
    }

    [Fact]
    public async Task ConfigurationUpdated_UnrelatedChange_EvaluatesAndDoesNotQueueTask()
    {
        _store.Save(_store.ComputeCurrent());
        await StartAndWaitForStartupCheck();
        var evaluation = NextEvaluation();

        _serverConfiguration.PreferredMetadataLanguage = "de";
        _configurationManager.Raise(x => x.ConfigurationUpdated += null, EventArgs.Empty);
        await evaluation;

        Assert.Equal(2, _host.EvaluationCount);
        VerifyQueued(Times.Never());
    }

    [Fact]
    public async Task LibraryOptionsUpdated_LibraryCountryChanged_QueuesTask()
    {
        var library = new VirtualFolderInfo { ItemId = "lib", Name = "lib", LibraryOptions = new LibraryOptions { MetadataCountryCode = "US" } };
        _libraryManager.Setup(x => x.GetVirtualFolders()).Returns([library]);
        _store.Save(_store.ComputeCurrent());
        await StartAndWaitForStartupCheck();
        var evaluation = NextEvaluation();

        library.LibraryOptions.MetadataCountryCode = "FR";
        _host.OnLibraryOptionsUpdated(null, new LibraryOptionsUpdatedEventArgs("/lib", library.LibraryOptions));
        await evaluation;

        VerifyQueued(Times.Once());
    }

    [Fact]
    public async Task LibraryOptionsUpdated_CountryUnchanged_EvaluatesAndDoesNotQueueTask()
    {
        var library = new VirtualFolderInfo { ItemId = "lib", Name = "lib", LibraryOptions = new LibraryOptions { MetadataCountryCode = "US" } };
        _libraryManager.Setup(x => x.GetVirtualFolders()).Returns([library]);
        _store.Save(_store.ComputeCurrent());
        await StartAndWaitForStartupCheck();
        var evaluation = NextEvaluation();

        library.LibraryOptions.EnableRealtimeMonitor = !library.LibraryOptions.EnableRealtimeMonitor;
        _host.OnLibraryOptionsUpdated(null, new LibraryOptionsUpdatedEventArgs("/lib", library.LibraryOptions));
        await evaluation;

        Assert.Equal(2, _host.EvaluationCount);
        VerifyQueued(Times.Never());
    }

    [Fact]
    public async Task TaskCompleted_RecomputeTaskCompletedAndStateStillStale_QueuesTask()
    {
        _store.Save(_store.ComputeCurrent());
        await StartAndWaitForStartupCheck();

        // A change made while the run was in progress.
        _serverConfiguration.MetadataCountryCode = "DE";
        RaiseTaskCompleted(TaskCompletionStatus.Completed);

        Assert.Equal(2, _host.EvaluationCount);
        VerifyQueued(Times.Once());
    }

    [Theory]
    [InlineData(TaskCompletionStatus.Failed, true)]
    [InlineData(TaskCompletionStatus.Cancelled, true)]
    [InlineData(TaskCompletionStatus.Completed, false)]
    public async Task TaskCompleted_FailedCancelledOrOtherTask_DoesNotEvaluate(TaskCompletionStatus status, bool recomputeTask)
    {
        await StartAndWaitForStartupCheck();
        var before = _host.EvaluationCount;

        RaiseTaskCompleted(status, recomputeTask);

        Assert.Equal(before, _host.EvaluationCount);
    }

    [Fact]
    public async Task AfterStop_EventsAreIgnored()
    {
        _store.Save(_store.ComputeCurrent());
        await StartAndWaitForStartupCheck();
        await _host.StopAsync(TestContext.Current.CancellationToken);

        _serverConfiguration.MetadataCountryCode = "DE";
        _configurationManager.Raise(x => x.ConfigurationUpdated += null, EventArgs.Empty);
        _host.OnLibraryOptionsUpdated(null, new LibraryOptionsUpdatedEventArgs("/lib", new LibraryOptions()));
        RaiseTaskCompleted(TaskCompletionStatus.Completed);

        Assert.False(_host.IsEvaluationScheduled);
        Assert.Equal(1, _host.EvaluationCount);
        VerifyQueued(Times.Never());
    }
}
