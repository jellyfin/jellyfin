using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.MediaEncoding.Transcoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class DynamicHlsSegmentTests : IAsyncLifetime
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("jellyfin-hls-segments-");
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private readonly List<Task<ActionResult>> _requests = [];
    private readonly Audio _item = new() { Id = Guid.NewGuid() };
    private readonly TranscodeManager _manager;
    private readonly TranscodingJob _job;
    private string _prefix = string.Empty;

    public DynamicHlsSegmentTests()
    {
        var configuration = new Mock<IServerConfigurationManager>();
        configuration.Setup(x => x.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = _directory.FullName });
        configuration.SetupGet(x => x.CommonApplicationPaths).Returns(Mock.Of<IApplicationPaths>());
        _fixture.Inject(configuration.Object);
        _fixture.Inject<ILoggerFactory>(NullLoggerFactory.Instance);
        _fixture.Inject<ILogger<DynamicHlsController>>(NullLogger<DynamicHlsController>.Instance);

        // The API test project has no production filesystem adapter; enumeration still reads real files.
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GetFilePaths(_directory.FullName, true)).Returns(Array.Empty<string>());
        fileSystem.Setup(x => x.GetFiles(_directory.FullName, It.IsAny<IReadOnlyList<string>>(), true, false))
            .Returns((string path, IReadOnlyList<string> extensions, bool caseSensitive, bool recursive) =>
                new DirectoryInfo(path).GetFiles()
                    .Where(file => extensions.Contains(file.Extension))
                    .Select(file => new FileSystemMetadata
                    {
                        Name = file.Name,
                        FullName = file.FullName,
                        LastWriteTimeUtc = file.LastWriteTimeUtc
                    }));
        fileSystem.Setup(x => x.GetLastWriteTimeUtc(It.IsAny<FileSystemMetadata>()))
            .Returns((FileSystemMetadata file) => file.LastWriteTimeUtc);
        _fixture.Inject(fileSystem.Object);

        var source = new MediaSourceInfo
        {
            Id = _item.Id.ToString("N"),
            Path = Path.Combine(_directory.FullName, "input.aac"),
            Container = "aac",
            Protocol = MediaProtocol.File,
            RunTimeTicks = TimeSpan.FromMinutes(2).Ticks,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Audio, Codec = "aac", Index = 0, Channels = 2, SampleRate = 48000, BitRate = 128000 }
            ]
        };
        var library = new Mock<ILibraryManager>();
        library.Setup(x => x.GetItemById<BaseItem>(_item.Id)).Returns(_item);
        _fixture.Inject(library.Object);
        var sources = new Mock<IMediaSourceManager>();
        sources.Setup(x => x.GetPlaybackMediaSources(_item, null, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        _fixture.Inject(sources.Object);
        _fixture.Inject(_fixture.Create<EncodingHelper>());
        _manager = _fixture.Create<TranscodeManager>();
        _fixture.Inject<ITranscodeManager>(_manager);
        _job = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
        {
            Type = TranscodingJobType.Hls,
            PlaySessionId = "session",
            MediaSource = source,
            CancellationTokenSource = new CancellationTokenSource()
        };
    }

    public async ValueTask InitializeAsync()
    {
        using var state = await StreamingHelpers.GetStreamingState(
            new StreamingRequestDto
            {
                Id = _item.Id,
                Container = "mp4",
                SegmentContainer = "mp4",
                AudioCodec = "aac",
                SegmentLength = 6,
                DeviceId = "device",
                PlaySessionId = "session"
            },
            Context(9, TestContext.Current.CancellationToken),
            _fixture.Create<IMediaSourceManager>(),
            _fixture.Create<IUserManager>(),
            _fixture.Create<ILibraryManager>(),
            _fixture.Create<IServerConfigurationManager>(),
            _fixture.Create<IMediaEncoder>(),
            _fixture.Create<EncodingHelper>(),
            _manager,
            TranscodingJobType.Hls,
            TestContext.Current.CancellationToken);
        _job.Path = Path.ChangeExtension(state.OutputFilePath, ".m3u8");
        _prefix = Path.Combine(_directory.FullName, Path.GetFileNameWithoutExtension(_job.Path));
        await WriteSegment(9);
        await WriteSegment(10);
        File.SetLastWriteTimeUtc(SegmentPath(9), DateTime.UtcNow.AddSeconds(-2));

        // Registration is private and otherwise starts FFmpeg; only controlled job seeding uses reflection.
        var activeJobs = (List<TranscodingJob>)typeof(TranscodeManager)
            .GetField("_activeTranscodingJobs", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_manager)!;
        activeJobs.Add(_job);
    }

    [Fact]
    public async Task ReadySegment_DoesNotWaitForLaterSegmentReadiness()
    {
        var later = Request(10, TestContext.Current.CancellationToken);
        Assert.Equal(1, _job.ActiveRequestCount);

        var ready = Request(9, TestContext.Current.CancellationToken);
        AssertSegment(9, await AwaitResult(ready.Result));
        Assert.False(later.Result.IsCompleted);
        Assert.Equal(2, _job.ActiveRequestCount);
        await ready.Complete();
        Assert.Equal(1, _job.ActiveRequestCount);

        await WriteSegment(11);
        AssertSegment(10, await AwaitResult(later.Result));
        await later.Complete();
        Assert.Equal(0, _job.ActiveRequestCount);
    }

    [Fact]
    public async Task CancelReadinessWait_ReleasesRequestWithoutCancelingSharedJob()
    {
        using var abort = new CancellationTokenSource();
        var request = Request(10, abort.Token);
        Assert.Equal(1, _job.ActiveRequestCount);

        await abort.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AwaitResult(request.Result));
        Assert.Equal(0, _job.ActiveRequestCount);
        Assert.False(_job.HasExited);
        Assert.False(_job.CancellationTokenSource!.IsCancellationRequested);
        var ready = Request(9, TestContext.Current.CancellationToken);
        AssertSegment(9, await AwaitResult(ready.Result));
        await ready.Complete();
        Assert.Equal(0, _job.ActiveRequestCount);
    }

    [Fact]
    public async Task CancelPlaylistLockWait_DoesNotRegisterRequest()
    {
        using var abort = new CancellationTokenSource();
        using (await _manager.LockAsync(_job.Path!, TestContext.Current.CancellationToken))
        {
            var request = Request(9, abort.Token);
            Assert.Equal(0, _job.ActiveRequestCount);
            await abort.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AwaitResult(request.Result));
        }

        Assert.Equal(0, _job.ActiveRequestCount);
        Assert.False(_job.CancellationTokenSource!.IsCancellationRequested);
    }

    [Fact]
    public async Task ResponseRegistrationFailure_ReleasesRequest()
    {
        var request = Request(9, TestContext.Current.CancellationToken, failResponseRegistration: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => AwaitResult(request.Result));

        Assert.Equal(0, _job.ActiveRequestCount);
        Assert.False(_job.CancellationTokenSource!.IsCancellationRequested);
    }

    [Fact]
    public async Task CancelReplacementWait_PreservesActiveReader()
    {
        var reader = Request(9, TestContext.Current.CancellationToken);
        AssertSegment(9, await AwaitResult(reader.Result));
        using var abort = new CancellationTokenSource();
        var replacement = Request(8, abort.Token);
        Assert.False(replacement.Result.IsCompleted);

        await abort.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AwaitResult(replacement.Result));
        Assert.Equal(1, _job.ActiveRequestCount);
        Assert.Same(_job, _manager.GetTranscodingJob(_job.Path!, TranscodingJobType.Hls));
        Assert.False(_job.CancellationTokenSource!.IsCancellationRequested);
        await reader.Complete();
        Assert.Equal(0, _job.ActiveRequestCount);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(-1)]
    public async Task ReplacementWait_AllowsReadersAndRechecksSegmentAfterTheyFinish(int segment)
    {
        var first = Request(9, TestContext.Current.CancellationToken);
        AssertSegment(9, await AwaitResult(first.Result));
        var replacement = Request(segment, TestContext.Current.CancellationToken);
        Assert.False(replacement.Result.IsCompleted);
        Assert.Equal(1, _job.ActiveRequestCount);
        Assert.Same(_job, _manager.GetTranscodingJob(_job.Path!, TranscodingJobType.Hls));

        var second = Request(9, TestContext.Current.CancellationToken);
        AssertSegment(9, await AwaitResult(second.Result));
        Assert.Equal(2, _job.ActiveRequestCount);
        await first.Complete();
        Assert.False(replacement.Result.IsCompleted);
        Assert.Equal(1, _job.ActiveRequestCount);

        await WriteSegment(segment);
        File.SetLastWriteTimeUtc(SegmentPath(segment), DateTime.UtcNow.AddSeconds(-2));
        await second.Complete();
        AssertSegment(segment, await AwaitResult(replacement.Result));
        Assert.False(_job.CancellationTokenSource!.IsCancellationRequested);
        Assert.Equal(1, _job.ActiveRequestCount);
        await replacement.Complete();
        Assert.Equal(0, _job.ActiveRequestCount);
    }

    [Fact]
    public async Task CompletedJob_ServesLastSegmentWithoutNextFile()
    {
        _job.HasExited = true;
        var request = Request(10, TestContext.Current.CancellationToken);

        AssertSegment(10, await AwaitResult(request.Result));

        Assert.Equal(1, _job.ActiveRequestCount);
        await request.Complete();
        Assert.Equal(0, _job.ActiveRequestCount);
    }

    public async ValueTask DisposeAsync()
    {
        _job.HasExited = true;
        _job.ActiveRequestCount = 0;
        try
        {
            await Task.WhenAll(_requests.Where(task => !task.IsCompleted)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        finally
        {
            _job.Dispose();
            _manager.Dispose();
            _directory.Delete(true);
        }
    }

    private static Task<ActionResult> AwaitResult(Task<ActionResult> task)
        // Deadlock guard only: tests assert ordering and ownership, not elapsed time.
        => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    private string SegmentPath(int segment) => _prefix + segment.ToString(CultureInfo.InvariantCulture) + ".mp4";

    private Task WriteSegment(int segment)
        => File.WriteAllTextAsync(SegmentPath(segment), "controlled segment", TestContext.Current.CancellationToken);

    private void AssertSegment(int segment, ActionResult result)
        => Assert.Equal(SegmentPath(segment), Assert.IsType<PhysicalFileResult>(result).FileName);

    private DefaultHttpContext Context(int segment, CancellationToken token)
    {
        var context = new DefaultHttpContext { RequestAborted = token };
        context.Request.Path = $"/Audio/{_item.Id}/hls1/main/{segment}.mp4";
        context.Request.Headers.UserAgent = "hls-segment-test";
        return context;
    }

    private (Task<ActionResult> Result, Func<Task> Complete) Request(int segment, CancellationToken token, bool failResponseRegistration = false)
    {
        var completions = new List<Func<Task>>();
        var response = new Mock<IHttpResponseFeature>();
        response.Setup(x => x.OnCompleted(It.IsAny<Func<object, Task>>(), It.IsAny<object>()))
            .Callback((Func<object, Task> callback, object state) =>
            {
                if (failResponseRegistration)
                {
                    throw new InvalidOperationException("Response registration failed");
                }

                completions.Add(() => callback(state));
            });
        var context = Context(segment, token);
        context.Features.Set(response.Object);
        var controller = _fixture.Build<DynamicHlsController>().OmitAutoProperties().Create();
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        var task = controller.GetHlsAudioSegment(
            itemId: _item.Id,
            playlistId: "main",
            segmentId: segment,
            container: "mp4",
            runtimeTicks: TimeSpan.FromSeconds(segment * 6).Ticks,
            actualSegmentLengthTicks: TimeSpan.FromSeconds(6).Ticks,
            @static: null,
            @params: null,
            tag: null,
            deviceProfileId: null,
            playSessionId: "session",
            segmentContainer: "mp4",
            segmentLength: 6,
            minSegments: null,
            mediaSourceId: null,
            deviceId: "device",
            audioCodec: "aac",
            enableAutoStreamCopy: null,
            allowVideoStreamCopy: null,
            allowAudioStreamCopy: null,
            audioSampleRate: null,
            maxAudioBitDepth: null,
            maxStreamingBitrate: null,
            audioBitRate: null,
            audioChannels: null,
            maxAudioChannels: null,
            profile: null,
            level: null,
            framerate: null,
            maxFramerate: null,
            copyTimestamps: null,
            startTimeTicks: null,
            width: null,
            height: null,
            videoBitRate: null,
            subtitleStreamIndex: null,
            subtitleMethod: null,
            maxRefFrames: null,
            maxVideoBitDepth: null,
            requireAvc: null,
            deInterlace: null,
            requireNonAnamorphic: null,
            transcodingMaxAudioChannels: null,
            cpuCoreLimit: null,
            liveStreamId: null,
            enableMpegtsM2TsMode: null,
            videoCodec: null,
            subtitleCodec: null,
            transcodeReasons: null,
            audioStreamIndex: null,
            videoStreamIndex: null,
            context: null,
            streamOptions: new Dictionary<string, string>());
        _requests.Add(task);
        return (task, async () =>
        {
            foreach (var callback in completions)
            {
                await callback();
            }
        });
    }
}
