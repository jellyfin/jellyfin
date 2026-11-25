using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The hls segment controller.
/// </summary>
[Route("")]
[ApiExplorerSettings(IgnoreApi = true)]
public class HlsSegmentController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ITranscodeManager _transcodeManager;
    private readonly VideoPlaybackHelper _videoPlaybackHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="HlsSegmentController"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    /// <param name="videoPlaybackHelper">The video playback admission helper.</param>
    public HlsSegmentController(
        IServerConfigurationManager serverConfigurationManager,
        ITranscodeManager transcodeManager,
        VideoPlaybackHelper videoPlaybackHelper)
    {
        _serverConfigurationManager = serverConfigurationManager;
        _transcodeManager = transcodeManager;
        _videoPlaybackHelper = videoPlaybackHelper;
    }

    /// <summary>
    /// Gets the specified audio segment for an audio item.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="segmentId">The segment id.</param>
    /// <response code="200">Hls audio segment returned.</response>
    /// <returns>A <see cref="FileStreamResult"/> containing the audio stream.</returns>
    // Can't require authentication just yet due to seeing some requests come from Chrome without full query string
    // [Authenticated]
    [HttpGet("Audio/{itemId}/hls/{segmentId}/stream.mp3", Name = "GetHlsAudioSegmentLegacyMp3")]
    [HttpGet("Audio/{itemId}/hls/{segmentId}/stream.aac", Name = "GetHlsAudioSegmentLegacyAac")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesAudioFile]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "itemId", Justification = "Required for ServiceStack")]
    public ActionResult GetHlsAudioSegmentLegacy([FromRoute, Required] string itemId, [FromRoute, Required] string segmentId)
    {
        // TODO: Deprecate with new iOS app
        var file = ValidateTranscodePath(string.Concat(segmentId, Path.GetExtension(Request.Path.Value.AsSpan())));
        if (file is null)
        {
            return BadRequest("Invalid segment.");
        }

        return FileStreamResponseHelpers.GetStaticFileResult(file, MimeTypes.GetMimeType(file));
    }

    /// <summary>
    /// Gets a hls video playlist.
    /// </summary>
    /// <param name="itemId">The video id.</param>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="200">Hls video playlist returned.</response>
    /// <returns>A <see cref="FileStreamResult"/> containing the playlist.</returns>
    [HttpGet("Videos/{itemId}/hls/{playlistId}/stream.m3u8")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesPlaylistFile]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "itemId", Justification = "Required for ServiceStack")]
    public ActionResult GetHlsPlaylistLegacy([FromRoute, Required] string itemId, [FromRoute, Required] string playlistId)
    {
        var file = ValidateTranscodePath(string.Concat(playlistId, Path.GetExtension(Request.Path.Value.AsSpan())));
        if (file is null
            || !Path.GetExtension(file.AsSpan()).Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid segment.");
        }

        return Guid.TryParse(itemId, out var id)
            ? GetVideoFileResult(id, file, file)
            : BadRequest("Invalid item.");
    }

    /// <summary>
    /// Stops an active encoding.
    /// </summary>
    /// <param name="deviceId">The device id of the client requesting. Used to stop encoding processes when needed.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <response code="204">Encoding stopped successfully.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpDelete("Videos/ActiveEncodings")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult StopEncodingProcess(
        [FromQuery, Required] string deviceId,
        [FromQuery, Required] string playSessionId)
    {
        var job = _transcodeManager.GetTranscodingJob(playSessionId);
        if (job is null)
        {
            return NoContent();
        }

        _videoPlaybackHelper.VerifyEncodingControl(HttpContext, playSessionId, job);

        _transcodeManager.KillTranscodingJobs(deviceId, playSessionId, _ => true);
        return NoContent();
    }

    /// <summary>
    /// Gets a hls video segment.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="segmentId">The segment id.</param>
    /// <param name="segmentContainer">The segment container.</param>
    /// <response code="200">Hls video segment returned.</response>
    /// <response code="404">Hls segment not found.</response>
    /// <returns>A <see cref="FileStreamResult"/> containing the video segment.</returns>
    [HttpGet("Videos/{itemId}/hls/{playlistId}/{segmentId}.{segmentContainer}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesVideoFile]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "itemId", Justification = "Required for ServiceStack")]
    public ActionResult GetHlsVideoSegmentLegacy(
        [FromRoute, Required] string itemId,
        [FromRoute, Required] string playlistId,
        [FromRoute, Required] string segmentId,
        [FromRoute, Required] string segmentContainer)
    {
        var file = ValidateTranscodePath(string.Concat(segmentId, Path.GetExtension(Request.Path.Value.AsSpan())));
        if (file is null)
        {
            return BadRequest("Invalid segment.");
        }

        var playlistPath = ValidateTranscodePath(string.Concat(playlistId, ".m3u8"));
        if (playlistPath is null || !Path.GetExtension(file).Equals(string.Concat(".", segmentContainer), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid segment.");
        }

        var prefix = Path.ChangeExtension(playlistPath, null);
        var segment = Path.ChangeExtension(file, null);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!segment.StartsWith(prefix, comparison)
            || !int.TryParse(segment.AsSpan(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            || index < -1)
        {
            return NotFound("Hls segment not found.");
        }

        return Guid.TryParse(itemId, out var id)
            ? GetVideoFileResult(id, file, playlistPath)
            : BadRequest("Invalid item.");
    }

    private string? ValidateTranscodePath(string filename)
    {
        var transcodePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_serverConfigurationManager.GetTranscodePath()));
        var file = Path.GetFullPath(filename, transcodePath);
        // Require a separator after the transcode path so a sibling like "<transcodePath>-evil" can't pass.
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!file.StartsWith(transcodePath + Path.DirectorySeparatorChar, comparison))
        {
            return null;
        }

        return file;
    }

    private ActionResult GetVideoFileResult(Guid itemId, string path, string playlistPath)
    {
        var job = _transcodeManager.GetTranscodingJob(playlistPath, TranscodingJobType.Hls);
        if (job is null)
        {
            return NotFound("Hls segment not found.");
        }

        if (string.IsNullOrWhiteSpace(job.PlaySessionId))
        {
            return User.GetIsApiKey() && User.GetUserId().Equals(Guid.Empty)
                && job.UserId.Equals(Guid.Empty) && job.ItemId.Equals(itemId)
                ? GetFileResult(path, playlistPath, job)
                : NotFound("Hls segment not found.");
        }

        _videoPlaybackHelper.VerifyPlaySession(HttpContext, job.PlaySessionId);
        using var request = _videoPlaybackHelper.Acquire(HttpContext, itemId, job.PlaySessionId);
        return VideoPlaybackHelper.Wrap(GetFileResult(path, playlistPath, job), request);
    }

    private ActionResult GetFileResult(string path, string playlistPath, TranscodingJob verifiedJob)
    {
        var transcodingJob = _transcodeManager.OnTranscodeBeginRequest(playlistPath, TranscodingJobType.Hls);
        if (!ReferenceEquals(transcodingJob, verifiedJob))
        {
            if (transcodingJob is not null)
            {
                _transcodeManager.OnTranscodeEndRequest(transcodingJob);
            }

            return NotFound("Hls segment not found.");
        }

        try
        {
            ActionResult result;
            if (string.Equals(path, playlistPath, StringComparison.Ordinal))
            {
                var text = HlsHelpers.AuthenticatePlaylistSegments(System.IO.File.ReadAllText(path), playlistPath, User.GetToken());
                Response.Headers.CacheControl = "private, no-store";
                result = Content(text, MimeTypes.GetMimeType(path));
            }
            else
            {
                result = FileStreamResponseHelpers.GetStaticFileResult(path, MimeTypes.GetMimeType(path));
            }

            Response.OnCompleted(() =>
            {
                _transcodeManager.OnTranscodeEndRequest(transcodingJob);
                return Task.CompletedTask;
            });

            return result;
        }
        catch
        {
            _transcodeManager.OnTranscodeEndRequest(transcodingJob);
            throw;
        }
    }
}
