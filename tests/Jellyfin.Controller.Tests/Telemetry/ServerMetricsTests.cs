using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Telemetry;
using MediaBrowser.Model.Tasks;
using Xunit;

namespace Jellyfin.Controller.Tests.Telemetry;

public class ServerMetricsTests
{
    private const string ClientTag = "jellyfin.client";
    private const string ItemKindTag = "jellyfin.item.kind";

    [Fact]
    public void Sessions_AreCountedPerClient_UntilTheyEnd()
    {
        const string Client = "ServerMetricsTests";
        const string SessionId = "server-metrics-tests-session";

        var started = MeterCollector.Collect(() => SessionMetrics.OnSessionStarted(SessionId, Client));

        Assert.Equal(1, MeterCollector.Value(started, "jellyfin.sessions.started", (ClientTag, Client)));
        Assert.Equal(1, MeterCollector.Value(started, "jellyfin.sessions.active", (ClientTag, Client)));

        // The client is remembered, so ending a session does not depend on the caller passing it again.
        var ended = MeterCollector.Collect(() => SessionMetrics.OnSessionEnded(SessionId, null));

        Assert.Equal(1, MeterCollector.Value(ended, "jellyfin.sessions.ended", (ClientTag, Client)));
        Assert.Equal(0, MeterCollector.Value(ended, "jellyfin.sessions.active", (ClientTag, Client)));
    }

    [Fact]
    public void AuthenticationAttempts_AreCountedByOutcome()
    {
        var measurements = MeterCollector.Collect(
            () => AuthenticationMetrics.OnAuthenticationAttempt(AuthenticationMetrics.OutcomeInvalidCredentials));

        Assert.Equal(
            1,
            MeterCollector.Value(
                measurements,
                "jellyfin.authentication.attempts",
                ("jellyfin.authentication.outcome", AuthenticationMetrics.OutcomeInvalidCredentials)));
    }

    [Fact]
    public void LibraryChanges_AreCountedByKind_AndSkipItemsOutsideTheLibrary()
    {
        var measurements = MeterCollector.Collect(() => LibraryMetrics.OnItemsAdded([new Audio(), new LiveTvProgram()]));

        // The live TV entry is left out, so only the library item is counted.
        Assert.Equal(
            1,
            MeterCollector.Value(measurements, "jellyfin.library.changes", ("jellyfin.library.change", "added"), (ItemKindTag, "Audio")));
    }

    [Fact]
    public void TaskCompletion_RecordsStatusAndDuration()
    {
        const string Key = "ServerMetricsTestsTask";

        var measurements = MeterCollector.Collect(
            () => TaskMetrics.OnTaskCompleted(Key, TaskCompletionStatus.Failed, TimeSpan.FromSeconds(90)));

        var tags = new[] { ("jellyfin.task.key", Key), ("jellyfin.task.status", nameof(TaskCompletionStatus.Failed)) };

        Assert.Equal(1, MeterCollector.Value(measurements, "jellyfin.task.executions", tags));
        Assert.Equal(90, MeterCollector.Value(measurements, TaskMetrics.TaskDurationName, tags), 0);
    }

    [Fact]
    public void MetadataRefresh_IsCountedWhileRunning_AndTimedWhenItCompletes()
    {
        long startedTimestamp = 0;

        var running = MeterCollector.Collect(() => startedTimestamp = ProviderMetrics.OnRefreshStarted());

        Assert.Equal(1, MeterCollector.Value(running, "jellyfin.metadata.refresh.active"));

        var completed = MeterCollector.Collect(() => ProviderMetrics.OnRefreshCompleted(startedTimestamp, BaseItemKind.Movie));

        Assert.Equal(0, MeterCollector.Value(completed, "jellyfin.metadata.refresh.active"));
        Assert.Single(completed, m => m.Matches(ProviderMetrics.RefreshDurationName, (ItemKindTag, "Movie")));
    }

    [Fact]
    public void MetadataRefresh_CountsConcurrentRefreshes()
    {
        var first = ProviderMetrics.OnRefreshStarted();
        var second = ProviderMetrics.OnRefreshStarted();

        var both = MeterCollector.Collect(() => { });
        Assert.Equal(2, MeterCollector.Value(both, "jellyfin.metadata.refresh.active"));

        ProviderMetrics.OnRefreshCompleted(first, BaseItemKind.Movie);
        ProviderMetrics.OnRefreshCompleted(second, BaseItemKind.Episode);

        var none = MeterCollector.Collect(() => { });
        Assert.Equal(0, MeterCollector.Value(none, "jellyfin.metadata.refresh.active"));
    }

    [Fact]
    public void SubtitleDownloads_AreCountedByProviderAndOutcome()
    {
        const string Provider = "ServerMetricsTestsProvider";

        var measurements = MeterCollector.Collect(() => ProviderMetrics.OnSubtitleDownload(Provider, false));

        Assert.Equal(
            1,
            MeterCollector.Value(measurements, "jellyfin.subtitle.downloads", ("jellyfin.provider", Provider), ("jellyfin.download.outcome", "failed")));
    }
}
