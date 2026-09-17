using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// External audio controller.
/// </summary>
[Route("Videos")]
[Tags("Video")]
public class VideoExternalAudioController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaSourceManager _mediaSourceManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoExternalAudioController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    public VideoExternalAudioController(
        ILibraryManager libraryManager,
        IMediaSourceManager mediaSourceManager)
    {
        _libraryManager = libraryManager;
        _mediaSourceManager = mediaSourceManager;
    }

    /// <summary>
    /// Gets an external audio file.
    /// </summary>
    /// <param name="videoId">Video ID.</param>
    /// <param name="mediaSourceId">Media Source ID.</param>
    /// <param name="index">Audio stream index.</param>
    /// <response code="200">Audio file returned.</response>
    /// <response code="404">Video, media source or external audio stream not found.</response>
    /// <returns>A <see cref="FileResult"/> with the audio file, or a <see cref="NotFoundResult"/>.</returns>
    [HttpGet("{videoId}/{mediaSourceId}/Audio/{index}/Stream")]
    [HttpHead("{videoId}/{mediaSourceId}/Audio/{index}/Stream", Name = "HeadExternalAudioStream")]
    [ProducesAudioFile]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetExternalAudio(
        [FromRoute, Required] Guid videoId,
        [FromRoute, Required] string mediaSourceId,
        [FromRoute, Required] int index)
    {
        var item = _libraryManager.GetItemById<Video>(videoId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        var audioStream = _mediaSourceManager.GetStaticMediaSources(item, false)
            .FirstOrDefault(i => string.Equals(i.Id, mediaSourceId, StringComparison.Ordinal))
            ?.MediaStreams
            .FirstOrDefault(i => i.Type == MediaStreamType.Audio && i.IsExternal && i.Index == index);
        if (audioStream?.Path is null)
        {
            return NotFound();
        }

        return PhysicalFile(audioStream.Path, MimeTypes.GetMimeType(audioStream.Path), true);
    }
}
