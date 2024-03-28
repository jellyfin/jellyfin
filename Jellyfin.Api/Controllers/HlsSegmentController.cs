using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The hls segment controller.
/// </summary>
[Route("")]
public class HlsSegmentController : BaseJellyfinApiController
{
    private readonly IFileSystem _fileSystem;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ITranscodeManager _transcodeManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="HlsSegmentController"/> class.
    /// </summary>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    public HlsSegmentController(
        IFileSystem fileSystem,
        IServerConfigurationManager serverConfigurationManager,
        ITranscodeManager transcodeManager)
    {
        _fileSystem = fileSystem;
        _serverConfigurationManager = serverConfigurationManager;
        _transcodeManager = transcodeManager;
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
        var file = string.Concat(segmentId, Path.GetExtension(Request.Path.Value.AsSpan()));
        var transcodePath = _serverConfigurationManager.GetTranscodePath();
        file = Path.GetFullPath(Path.Combine(transcodePath, file));
        var fileDir = Path.GetDirectoryName(file);
        if (string.IsNullOrEmpty(fileDir) || !fileDir.StartsWith(transcodePath, StringComparison.InvariantCulture))
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
        var file = string.Concat(playlistId, Path.GetExtension(Request.Path.Value.AsSpan()));
        var transcodePath = _serverConfigurationManager.GetTranscodePath();
        file = Path.GetFullPath(Path.Combine(transcodePath, file));
        var fileDir = Path.GetDirectoryName(file);
        if (string.IsNullOrEmpty(fileDir) || !fileDir.StartsWith(transcodePath, StringComparison.InvariantCulture)
            || Path.GetExtension(file.AsSpan()).Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid segment.");
        }

        return GetFileResult(file, file);
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
    // Can't require authentication just yet due to seeing some requests come from Chrome without full query string
    // [Authenticated]
    [HttpGet("Videos/{itemId}/hls/{playlistId}/{segmentId}.{segmentContainer}")]
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
        var file = string.Concat(segmentId, Path.GetExtension(Request.Path.Value.AsSpan()));
        var transcodeFolderPath = _serverConfigurationManager.GetTranscodePath();

        file = Path.GetFullPath(Path.Combine(transcodeFolderPath, file));
        var fileDir = Path.GetDirectoryName(file);
        if (string.IsNullOrEmpty(fileDir) || !fileDir.StartsWith(transcodeFolderPath, StringComparison.InvariantCulture))
        {
            return BadRequest("Invalid segment.");
        }

        var normalizedPlaylistId = playlistId;

        var filePaths = _fileSystem.GetFilePaths(transcodeFolderPath);
        // Add . to start of segment container for future use.
        segmentContainer = segmentContainer.Insert(0, ".");
        string? playlistPath = null;
        foreach (var path in filePaths)
        {
            var pathExtension = Path.GetExtension(path);
            if ((string.Equals(pathExtension, segmentContainer, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(pathExtension, ".m3u8", StringComparison.OrdinalIgnoreCase))
                && path.Contains(normalizedPlaylistId, StringComparison.OrdinalIgnoreCase))
            {
                playlistPath = path;
                break;
            }
        }

        return playlistPath is null
            ? NotFound("Hls segment not found.")
            : GetFileResult(file, playlistPath);
    }

    private ActionResult GetFileResult(string path, string playlistPath)
    {
        var transcodingJob = _transcodeManager.OnTranscodeBeginRequest(playlistPath, TranscodingJobType.Hls);

        Response.OnCompleted(() =>
        {
            if (transcodingJob is not null)
            {
                _transcodeManager.OnTranscodeEndRequest(transcodingJob);
            }

            return Task.CompletedTask;
        });

        return FileStreamResponseHelpers.GetStaticFileResult(path, MimeTypes.GetMimeType(path));
    }
}
