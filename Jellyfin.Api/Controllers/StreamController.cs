using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The stream controller.
/// </summary>
public class StreamController : BaseJellyfinApiController
{
    private readonly MediaInfoController _mediaInfoController;
    private readonly ILibraryManager _libraryManager;
    private readonly IEnumerable<IStreamProvider> _streamProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamController"/> class.
    /// </summary>
    /// <param name="mediaInfoController">Media info controller holding the cached playback info.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="streamProviders">All available stream providers.</param>
    public StreamController(MediaInfoController mediaInfoController, ILibraryManager libraryManager, IEnumerable<IStreamProvider> streamProviders)
    {
        _mediaInfoController = mediaInfoController;
        _libraryManager = libraryManager;
        _streamProviders = streamProviders;
    }

    /// <summary>
    /// Gets a progressive stream by selecting a compatible stream provider.
    /// </summary>
    /// <param name="playSessionId">The play session ID for a given progressive stream request.</param>
    /// <returns>An <see cref="ActionResult"/>.</returns>
    [HttpGet("Progressive")]
    [ProducesBookFile]
    public async Task<ActionResult> GetProgressiveStream([FromQuery] string playSessionId)
    {
        if (_mediaInfoController.GetCachedPlaybackInfo(playSessionId) is not var (playbackInfo, deviceProfile))
        {
            return NotFound();
        }

        var mediaSource = playbackInfo.MediaSources.Count > 0 ? playbackInfo.MediaSources[0] : null;
        if (mediaSource is null || !Guid.TryParse(mediaSource.Id, out var itemId))
        {
            return NotFound();
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        if (item.MediaType == MediaType.Video || item.MediaType == MediaType.Audio)
        {
            // TODO migrate all progressive audio and video streams to this endpoint
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        if (mediaSource.TranscodingSubProtocol == MediaStreamProtocol.hls)
        {
            // this endpoint only returns progressive streams
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        var provider = _streamProviders.FirstOrDefault(p => p.StreamProtocol == MediaStreamProtocol.http && p.Supports(item, deviceProfile));
        if (provider is null)
        {
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        var stream = await provider.Stream(mediaSource, playSessionId).ConfigureAwait(false);
        return File(stream, MimeTypes.GetMimeType(item.Path ?? string.Empty), item.Path);
    }
}
