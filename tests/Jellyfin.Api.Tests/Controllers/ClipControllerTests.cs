using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models.ClipDtos;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public sealed class ClipControllerTests : IDisposable
{
    private readonly ClipController _subject;
    private readonly Mock<ILibraryManager> _mockLibraryManager;

    public ClipControllerTests()
    {
        _mockLibraryManager = new Mock<ILibraryManager>();
        var mockUserManager = new Mock<IUserManager>();
        var mockMediaSourceManager = new Mock<IMediaSourceManager>();
        var mockServerConfigurationManager = new Mock<IServerConfigurationManager>();
        var mockMediaEncoder = new Mock<IMediaEncoder>();
        var mockLogger = new Mock<ILogger<ClipController>>();

        _subject = new ClipController(
            _mockLibraryManager.Object,
            mockUserManager.Object,
            mockMediaSourceManager.Object,
            mockServerConfigurationManager.Object,
            mockMediaEncoder.Object,
            mockLogger.Object);

        _subject.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    public void Dispose()
    {
        foreach (var job in ClipController.ClipJobs.Values)
        {
            job.CancellationTokenSource.Dispose();
        }

        ClipController.ClipJobs.Clear();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static ClipJob MakeJob(string clipId, Guid? itemId = null, bool isComplete = false, bool hasError = false)
        => new ClipJob
        {
            ClipId = clipId,
            ItemId = itemId ?? Guid.NewGuid(),
            OutputPath = "/tmp/fake-clip.mp4",
            ItemName = "Test Movie",
            StartTimeTicks = 0,
            EndTimeTicks = 10_000_000_000,
            DurationTicks = 10_000_000_000,
            IsComplete = isComplete,
            HasError = hasError,
            ErrorMessage = hasError ? "FFmpeg exited with code 1" : null,
            CancellationTokenSource = new CancellationTokenSource()
        };

    // ── CreateClip validation tests ───────────────────────────────────

    [Fact]
    public async Task CreateClip_EndBeforeStart_ReturnsBadRequest()
    {
        var result = await _subject.CreateClip(
            Guid.NewGuid(),
            startTimeTicks: 5_000_000_000,
            endTimeTicks: 1_000_000_000,
            mediaSourceId: null,
            audioStreamIndex: null,
            videoCodec: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateClip_EqualStartAndEnd_ReturnsBadRequest()
    {
        var result = await _subject.CreateClip(
            Guid.NewGuid(),
            startTimeTicks: 5_000_000_000,
            endTimeTicks: 5_000_000_000,
            mediaSourceId: null,
            audioStreamIndex: null,
            videoCodec: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateClip_NonexistentItem_ReturnsNotFound()
    {
        _mockLibraryManager
            .Setup(m => m.GetItemById<BaseItem>(It.IsAny<Guid>(), (User?)null))
            .Returns((BaseItem?)null);

        var result = await _subject.CreateClip(
            Guid.NewGuid(),
            startTimeTicks: 0,
            endTimeTicks: 10_000_000_000,
            mediaSourceId: null,
            audioStreamIndex: null,
            videoCodec: null);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── CreateClip concurrency tests ──────────────────────────────────

    [Fact]
    public async Task CreateClip_WhenJobAlreadyRunning_Returns429()
    {
        var semaphoreField = typeof(ClipController)
            .GetField("EncodingSemaphore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var semaphore = (SemaphoreSlim)semaphoreField.GetValue(null)!;

        semaphore.Wait(0);
        try
        {
            var result = await _subject.CreateClip(
                Guid.NewGuid(),
                startTimeTicks: 0,
                endTimeTicks: 10_000_000_000,
                mediaSourceId: null,
                audioStreamIndex: null,
                videoCodec: null);

            Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status429TooManyRequests, ((ObjectResult)result).StatusCode);
        }
        finally
        {
            semaphore.Release();
        }
    }

    // ── DownloadClip tests ────────────────────────────────────────────

    [Fact]
    public void DownloadClip_UnknownClipId_ReturnsNotFound()
    {
        var result = _subject.DownloadClip(Guid.NewGuid(), "nonexistent-clip-id");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void DownloadClip_WrongItemId_ReturnsNotFound()
    {
        var correctItemId = Guid.NewGuid();
        var wrongItemId = Guid.NewGuid();
        var clipId = "test-wrong-item";

        ClipController.ClipJobs[clipId] = MakeJob(clipId, itemId: correctItemId, isComplete: true);

        var result = _subject.DownloadClip(wrongItemId, clipId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void DownloadClip_StillInProgress_ReturnsConflict()
    {
        var itemId = Guid.NewGuid();
        var clipId = "test-in-progress";

        ClipController.ClipJobs[clipId] = MakeJob(clipId, itemId: itemId);

        var result = _subject.DownloadClip(itemId, clipId);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void DownloadClip_FailedJob_ReturnsBadRequest()
    {
        var itemId = Guid.NewGuid();
        var clipId = "test-failed";

        ClipController.ClipJobs[clipId] = MakeJob(clipId, itemId: itemId, hasError: true);

        var result = _subject.DownloadClip(itemId, clipId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── CancelClip tests ──────────────────────────────────────────────

    [Fact]
    public void CancelClip_UnknownClipId_ReturnsNotFound()
    {
        var result = _subject.CancelClip(Guid.NewGuid(), "nonexistent-clip-id");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void CancelClip_WrongItemId_ReturnsNotFound()
    {
        var correctItemId = Guid.NewGuid();
        var wrongItemId = Guid.NewGuid();
        var clipId = "test-cancel-wrong-item";

        ClipController.ClipJobs[clipId] = MakeJob(clipId, itemId: correctItemId);

        var result = _subject.CancelClip(wrongItemId, clipId);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.True(ClipController.ClipJobs.ContainsKey(clipId), "Job should remain when itemId does not match");
    }

    [Fact]
    public void CancelClip_ValidJob_ReturnsNoContentAndRemovesJob()
    {
        var itemId = Guid.NewGuid();
        var clipId = "test-cancel-valid";

        ClipController.ClipJobs[clipId] = MakeJob(clipId, itemId: itemId);

        var result = _subject.CancelClip(itemId, clipId);

        Assert.IsType<NoContentResult>(result);
        Assert.False(ClipController.ClipJobs.ContainsKey(clipId), "Job should be removed after cancellation");
    }

    // ── GetClipProgress tests ─────────────────────────────────────────

    [Fact]
    public async Task GetClipProgress_UnknownClipId_ReturnsNotFound()
    {
        var result = await _subject.GetClipProgress(Guid.NewGuid(), "nonexistent", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetClipProgress_WrongItemId_ReturnsNotFound()
    {
        var correctItemId = Guid.NewGuid();
        var clipId = "test-progress-wrong-item";

        ClipController.ClipJobs[clipId] = MakeJob(clipId, itemId: correctItemId);

        var result = await _subject.GetClipProgress(Guid.NewGuid(), clipId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
