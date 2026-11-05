using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public sealed class OfflineDownloadJobsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-offline-test-" + Guid.NewGuid().ToString("N"));
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _item = Guid.NewGuid();
    private DateTimeOffset _now = DateTimeOffset.UtcNow;
    private OfflineDownloadJobs? _jobs;

    private OfflineDownloadJobs Jobs => _jobs ??= NewJobs();

    private OfflineDownloadJobs NewJobs() => new(_root, () => _now, () => 10L * 1024 * 1024 * 1024);

    private OfflineDownloadJobInfo Create(string quality = "720p", string fingerprint = "source-1")
        => Jobs.Create(_owner, _item, fingerprint, "video.mp4", quality, 1, null, "none");

    private Task Ready(Guid id) => Jobs.Prepare(_owner, _item, id, _ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("synthetic-completed-mp4-bytes"))));

    [Fact]
    public void ExpiryIsAbsolute24Hours()
    {
        var job = Create();
        Assert.Equal(_now.AddHours(24), job.ExpiresAt);
        _now = _now.AddHours(23);
        Assert.Equal(job.Id, Create().Id);
        Assert.Equal(job.ExpiresAt, Create().ExpiresAt);
        _now = _now.AddHours(1);
        Assert.Null(Jobs.Get(_owner, _item, job.Id));
        Assert.Empty(Directory.GetDirectories(Path.Combine(_root, "offline-downloads-v2")));
    }

    [Fact]
    public async Task ExpiryTimerRunsWithoutAStatusOrDownloadRequest()
    {
        Create();
        _now = _now.AddHours(24).AddSeconds(1);
        await Task.Delay(TimeSpan.FromSeconds(32), TestContext.Current.CancellationToken);
        Assert.Empty(Directory.GetDirectories(Path.Combine(_root, "offline-downloads-v2")));
    }

    [Fact]
    public void OwnershipAndItemAreBothRequired()
    {
        var job = Create();
        Assert.Null(Jobs.Get(Guid.NewGuid(), _item, job.Id));
        Assert.Null(Jobs.Get(_owner, Guid.NewGuid(), job.Id));
        Assert.False(Jobs.Delete(Guid.NewGuid(), _item, job.Id));
        Assert.Empty(Jobs.List(Guid.NewGuid(), _item));
        Assert.NotNull(Jobs.Get(_owner, _item, job.Id));
    }

    [Fact]
    public void QuotaReservesQueuedAndReadySlots()
    {
        var first = Create();
        Create("540p");
        Assert.Throws<InvalidOperationException>(() => Create("1080p"));
        Assert.True(Jobs.Delete(_owner, _item, first.Id));
        Assert.NotNull(Create("1080p"));
    }

    [Fact]
    public void ChangedSourceInvalidatesPreviousBytes()
    {
        var job = Create();
        Assert.Null(Jobs.Get(_owner, _item, job.Id, "changed-source"));
        Assert.Empty(Jobs.List(_owner, _item));
    }

    [Fact]
    public async Task ReadyFileHasStableIdentityAndSeekableBytes()
    {
        var job = Create();
        await Ready(job.Id);
        var ready = Jobs.Get(_owner, _item, job.Id)!;
        Assert.Equal("Ready", ready.Status);
        Assert.StartsWith("\"", ready.ETag, StringComparison.Ordinal);
        Assert.Equal(66, ready.ETag!.Length);
        await using var stream = Jobs.Open(_owner, _item, job.Id);
        Assert.True(stream.CanSeek);
        Assert.Equal(ready.Size, stream.Length);
        stream.Seek(10, SeekOrigin.Begin);
        var bytes = new byte[8];
        Assert.Equal(8, await stream.ReadAsync(bytes, TestContext.Current.CancellationToken));
        Assert.Equal("complete", Encoding.UTF8.GetString(bytes));
        Assert.Equal(ready.ETag, Jobs.Get(_owner, _item, job.Id)!.ETag);
    }

    [Fact]
    public async Task DeleteDefersUnlinkUntilReaderDisposesAndAbortsHttpTransfer()
    {
        var job = Create();
        await Ready(job.Id);
        var aborted = false;
        var stream = Jobs.Open(_owner, _item, job.Id, () => aborted = true);
        Assert.True(Jobs.Delete(_owner, _item, job.Id));
        Assert.True(aborted);
        Assert.Null(Jobs.Get(_owner, _item, job.Id));
        Assert.Single(Directory.GetDirectories(Path.Combine(_root, "offline-downloads-v2")));
        await stream.DisposeAsync();
        Assert.Empty(Directory.GetDirectories(Path.Combine(_root, "offline-downloads-v2")));
    }

    [Fact]
    public async Task ReadyJobSurvivesRestartWithIdenticalEtag()
    {
        var job = Create();
        await Ready(job.Id);
        var etag = Jobs.Get(_owner, _item, job.Id)!.ETag;
        Jobs.Dispose();
        _jobs = NewJobs();
        Assert.Equal(etag, Jobs.Get(_owner, _item, job.Id)!.ETag);
    }

    [Fact]
    public void IncompleteJobsDoNotSurviveRestart()
    {
        var job = Create();
        Jobs.Dispose();
        _jobs = NewJobs();
        Assert.Null(Jobs.Get(_owner, _item, job.Id));
        Assert.Empty(Directory.GetDirectories(Path.Combine(_root, "offline-downloads-v2")));
    }

    [Fact]
    public async Task CorruptCompletedCacheIsDiscardedOnRestart()
    {
        var job = Create();
        await Ready(job.Id);
        Jobs.Dispose();
        await File.WriteAllTextAsync(Path.Combine(_root, "offline-downloads-v2", job.Id.ToString("N"), "download.mp4"), new string('x', 29), TestContext.Current.CancellationToken);
        _jobs = NewJobs();
        Assert.Null(Jobs.Get(_owner, _item, job.Id));
    }

    [Fact]
    public async Task FailedEncoderNeverPublishesPartialFile()
    {
        var job = Create();
        await Jobs.Prepare(_owner, _item, job.Id, _ => throw new IOException("private path must not escape"));
        var failed = Jobs.Get(_owner, _item, job.Id)!;
        Assert.Equal("Failed", failed.Status);
        Assert.DoesNotContain("private path", failed.Error, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => Jobs.Open(_owner, _item, job.Id));
        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "offline-downloads-v2"), "*.part", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task GlobalEncoderIsSerializedAndQueuedCancelDoesNotOpenSource()
    {
        var first = Create();
        var second = Create("540p");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstTask = Jobs.Prepare(_owner, _item, first.Id, async token =>
        {
            entered.SetResult();
            await release.Task.WaitAsync(token);
            return new MemoryStream(new byte[] { 1, 2, 3 });
        });
        await entered.Task;
        var secondOpened = false;
        var secondTask = Jobs.Prepare(_owner, _item, second.Id, _ =>
        {
            secondOpened = true;
            return Task.FromResult<Stream>(new MemoryStream(new byte[] { 4 }));
        });
        Assert.False(secondOpened);
        Jobs.Delete(_owner, _item, second.Id);
        release.SetResult();
        await Task.WhenAll(firstTask, secondTask);
        Assert.False(secondOpened);
        Assert.Equal("Ready", Jobs.Get(_owner, _item, first.Id)!.Status);
        Assert.Null(Jobs.Get(_owner, _item, second.Id));
    }

    [Fact]
    public async Task PreparationIsNotRepeatedForReadyJob()
    {
        var job = Create();
        await Ready(job.Id);
        await Jobs.Prepare(_owner, _item, job.Id, _ => throw new InvalidOperationException("must not reopen"));
        Assert.Equal("Ready", Jobs.Get(_owner, _item, job.Id)!.Status);
    }

    [Fact]
    public void ExplicitTrackChoicesDoNotDeduplicateTogether()
    {
        var first = Create();
        var alternate = Jobs.Create(_owner, _item, "source-1", "video.mp4", "720p", 2, 4, "embed");
        Assert.NotEqual(first.Id, alternate.Id);
    }

    public void Dispose()
    {
        _jobs?.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
