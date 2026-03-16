using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.ClipDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Controller for extracting video clips with frame-accurate re-encoding.
/// Uses the source codec, bitrate and resolution — no pipeline downscaling.
/// </summary>
[Authorize(Policy = Policies.Download)]
public class ClipController : BaseJellyfinApiController
{
    /// <summary>Maximum number of concurrent FFmpeg clip encoding jobs (per server).</summary>
    private static readonly SemaphoreSlim EncodingSemaphore = new(1, 1);

    internal static readonly ConcurrentDictionary<string, ClipJob> ClipJobs = new();

    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ILogger<ClipController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ClipController}"/> interface.</param>
    public ClipController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IMediaSourceManager mediaSourceManager,
        IServerConfigurationManager serverConfigurationManager,
        IMediaEncoder mediaEncoder,
        ILogger<ClipController> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _mediaSourceManager = mediaSourceManager;
        _serverConfigurationManager = serverConfigurationManager;
        _mediaEncoder = mediaEncoder;
        _logger = logger;
    }

    /// <summary>
    /// Creates a clip extraction job from a video.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="startTimeTicks">Start time in ticks (10,000 ticks = 1 ms).</param>
    /// <param name="endTimeTicks">End time in ticks.</param>
    /// <param name="mediaSourceId">Optional media source id.</param>
    /// <param name="audioStreamIndex">Optional 0-based audio stream index to include in the clip.</param>
    /// <param name="videoCodec">Target video codec: "h264" (default), "h265", or "av1".</param>
    /// <response code="200">Clip job created, returns clipId.</response>
    /// <response code="400">Invalid time range or encoding error.</response>
    /// <response code="404">Item not found.</response>
    /// <response code="429">An encoding job is already in progress.</response>
    /// <returns>A clip job id and estimated duration.</returns>
    [HttpPost("/Videos/{itemId}/Clip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> CreateClip(
        [FromRoute, Required] Guid itemId,
        [FromQuery, Required] long startTimeTicks,
        [FromQuery, Required] long endTimeTicks,
        [FromQuery] string? mediaSourceId,
        [FromQuery] int? audioStreamIndex,
        [FromQuery] string? videoCodec)
    {
        if (endTimeTicks <= startTimeTicks)
        {
            return BadRequest("endTimeTicks must be greater than startTimeTicks.");
        }

        if (!await EncodingSemaphore.WaitAsync(0).ConfigureAwait(false))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, "An encoding job is already in progress.");
        }

        try
        {
            return await CreateClipInternal(itemId, startTimeTicks, endTimeTicks, mediaSourceId, audioStreamIndex, videoCodec).ConfigureAwait(false);
        }
        catch
        {
            EncodingSemaphore.Release();
            throw;
        }
    }

    private async Task<ActionResult> CreateClipInternal(
        Guid itemId,
        long startTimeTicks,
        long endTimeTicks,
        string? mediaSourceId,
        int? audioStreamIndex,
        string? videoCodec)
    {
        var userId = User.GetUserId();
        var user = userId.IsEmpty() ? null : _userManager.GetUserById(userId);
        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        var source = await ResolveMediaSourceAsync(item, user, mediaSourceId).ConfigureAwait(false);
        if (source is null)
        {
            return NotFound("Media source not found.");
        }

        var inputPath = source.Path;
        if (string.IsNullOrEmpty(inputPath))
        {
            return BadRequest("Media source has no path.");
        }

        var (videoStream, selectedAudioStream, ffmpegAudioIndex) = FindStreams(source, audioStreamIndex);

        if (videoStream is null)
        {
            return BadRequest("No video stream found in media source.");
        }

        var (videoEncoder, container, audioEncoder) = ClipEncoderResolver.ResolveEncoders(
            _mediaEncoder,
            videoCodec ?? "h264");

        // Build output path
        var transcodePath = _serverConfigurationManager.GetTranscodePath();
        var clipId = Guid.NewGuid().ToString("N");
        var outputPath = Path.Combine(transcodePath, $"clip-{clipId}.{container}");

        // Build FFmpeg arguments
        var startSeconds = startTimeTicks / 10_000_000.0;
        var durationSeconds = (endTimeTicks - startTimeTicks) / 10_000_000.0;

        var ffmpegArgs = ClipFfmpegArgsBuilder.Build(new ClipFfmpegOptions
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            StartSeconds = startSeconds,
            DurationSeconds = durationSeconds,
            VideoEncoder = videoEncoder,
            AudioEncoder = audioEncoder,
            VideoBitRate = videoStream.BitRate,
            AudioBitRate = selectedAudioStream?.BitRate,
            Container = container,
            AudioStreamIndex = ffmpegAudioIndex
        });

        _logger.LogInformation(
            "Clip extraction starting: {ClipId} for item {ItemId} ({Start} -> {End}), codec={VideoEncoder}, container={Container}",
            clipId,
            itemId,
            TimeSpan.FromTicks(startTimeTicks),
            TimeSpan.FromTicks(endTimeTicks),
            videoEncoder,
            container);

        _logger.LogInformation("FFmpeg clip command: ffmpeg {Args}", ffmpegArgs);

        // Start FFmpeg as a background process, track progress via stderr parsing
        var cts = new CancellationTokenSource();
        var clipJob = new ClipJob
        {
            ClipId = clipId,
            ItemId = itemId,
            OutputPath = outputPath,
            ItemName = item.Name,
            StartTimeTicks = startTimeTicks,
            EndTimeTicks = endTimeTicks,
            DurationTicks = endTimeTicks - startTimeTicks,
            CancellationTokenSource = cts
        };

        ClipJobs[clipId] = clipJob;

        var runner = new ClipFfmpegRunner(_mediaEncoder, _logger);
        _ = runner.RunAsync(clipJob, ffmpegArgs, ClipJobs, EncodingSemaphore, cts.Token);

        return new OkObjectResult(new ClipCreationResponseDto
        {
            ClipId = clipId,
            EstimatedDurationSeconds = durationSeconds
        });
    }

    private async Task<MediaSourceInfo?> ResolveMediaSourceAsync(
        BaseItem item,
        User? user,
        string? mediaSourceId)
    {
        var mediaSources = await _mediaSourceManager
            .GetPlaybackMediaSources(item, user, true, false, CancellationToken.None)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(mediaSourceId))
        {
            return mediaSources.Count > 0 ? mediaSources[0] : null;
        }

        return mediaSources.FirstOrDefault(s => string.Equals(s.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase));
    }

    private (MediaStream? VideoStream, MediaStream? AudioStream, int FfmpegAudioIndex) FindStreams(
        MediaSourceInfo source,
        int? audioStreamIndex)
    {
        MediaStream? videoStream = null;
        MediaStream? selectedAudioStream = null;
        var ffmpegAudioIndex = 0;
        var audioCounter = 0;

        foreach (var stream in source.MediaStreams)
        {
            if (videoStream is null && stream.Type == MediaStreamType.Video)
            {
                videoStream = stream;
            }

            if (stream.Type == MediaStreamType.Audio)
            {
                selectedAudioStream ??= stream;

                if (audioStreamIndex.HasValue && stream.Index == audioStreamIndex.Value)
                {
                    selectedAudioStream = stream;
                    ffmpegAudioIndex = audioCounter;
                }

                audioCounter++;
            }
        }

        if (audioStreamIndex.HasValue
            && selectedAudioStream is not null
            && selectedAudioStream.Index != audioStreamIndex.Value)
        {
            _logger.LogWarning(
                "Audio stream index {Requested} not found in source, falling back to first audio stream (index {Fallback}).",
                audioStreamIndex.Value,
                selectedAudioStream.Index);
        }

        return (videoStream, selectedAudioStream, ffmpegAudioIndex);
    }

    /// <summary>
    /// Gets the progress of a clip extraction job via Server-Sent Events.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="clipId">The clip job id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">SSE stream of progress events.</response>
    /// <response code="404">Clip job not found.</response>
    /// <returns>A SSE stream with progress updates.</returns>
    [HttpGet("/Videos/{itemId}/Clip/{clipId}/Progress")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetClipProgress(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] string clipId,
        CancellationToken cancellationToken)
    {
        if (!ClipJobs.TryGetValue(clipId, out var clipJob) || !clipJob.ItemId.Equals(itemId))
        {
            return NotFound();
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        while (!cancellationToken.IsCancellationRequested)
        {
            string eventName;
            string eventData;

            if (clipJob.HasError)
            {
                eventName = "error";
                eventData = clipJob.ErrorMessage ?? "FFmpeg failed";
            }
            else if (clipJob.IsComplete)
            {
                eventName = "complete";
                eventData = "done";
            }
            else
            {
                eventName = "progress";
                eventData = clipJob.ProgressPercent.ToString("F1", CultureInfo.InvariantCulture);
            }

            await Response.WriteAsync($"event: {eventName}\ndata: {eventData}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (clipJob.IsComplete || clipJob.HasError)
            {
                break;
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return Ok();
    }

    /// <summary>
    /// Downloads a completed clip.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="clipId">The clip job id.</param>
    /// <response code="200">Clip file returned.</response>
    /// <response code="404">Clip job not found.</response>
    /// <response code="409">Clip is not ready yet.</response>
    /// <returns>A <see cref="FileResult"/> containing the clip file.</returns>
    [HttpGet("/Videos/{itemId}/Clip/{clipId}/Download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult DownloadClip(
        [FromRoute] Guid itemId,
        [FromRoute, Required] string clipId)
    {
        if (!ClipJobs.TryGetValue(clipId, out var clipJob) || !clipJob.ItemId.Equals(itemId))
        {
            return NotFound("Clip job not found.");
        }

        if (clipJob.HasError)
        {
            return BadRequest(clipJob.ErrorMessage ?? "Clip extraction failed.");
        }

        if (!clipJob.IsComplete)
        {
            return Conflict("Clip extraction is still in progress.");
        }

        var filePath = clipJob.OutputPath;
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return NotFound("Clip file not found on disk.");
        }

        var contentType = MimeTypes.GetMimeType(filePath);

        // Build a meaningful download filename
        var startTime = TimeSpan.FromTicks(clipJob.StartTimeTicks);
        var endTime = TimeSpan.FromTicks(clipJob.EndTimeTicks);
        var safeName = (clipJob.ItemName ?? "clip")
            .Replace("\"", string.Empty, StringComparison.Ordinal)
            .Replace("/", "-", StringComparison.Ordinal);
        var ext = Path.GetExtension(filePath);
        var filename = FormattableString.Invariant($"{safeName}_{startTime:hh\\.mm\\.ss}-{endTime:hh\\.mm\\.ss}{ext}");

        var cd = new ContentDispositionHeaderValue("attachment")
        {
            FileNameStar = filename
        };
        Response.Headers.ContentDisposition = cd.ToString();

        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Cancels an in-progress clip extraction job and removes it.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="clipId">The clip job id.</param>
    /// <response code="204">Job cancelled successfully.</response>
    /// <response code="404">Clip job not found.</response>
    /// <returns>No content.</returns>
    [HttpDelete("/Videos/{itemId}/Clip/{clipId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult CancelClip(
        [FromRoute] Guid itemId,
        [FromRoute, Required] string clipId)
    {
        if (!ClipJobs.TryGetValue(clipId, out var clipJob) || !clipJob.ItemId.Equals(itemId))
        {
            return NotFound("Clip job not found.");
        }

        try
        {
            clipJob.CancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already finished — nothing to cancel
        }
        finally
        {
            clipJob.CancellationTokenSource.Dispose();
        }

        ClipJobs.TryRemove(clipId, out _);

        _logger.LogInformation("Clip job {ClipId} cancelled by user.", clipId);
        return NoContent();
    }
}
