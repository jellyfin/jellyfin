using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Lyrics controller.
/// </summary>
[Route("")]
public class LyricsController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILyricManager _lyricManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LyricsController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="lyricManager">Instance of the <see cref="ILyricManager"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public LyricsController(
        ILibraryManager libraryManager,
        ILyricManager lyricManager,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        IUserManager userManager)
    {
        _libraryManager = libraryManager;
        _lyricManager = lyricManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets an item's lyrics.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <response code="200">Lyrics returned.</response>
    /// <response code="404">Something went wrong. No Lyrics will be returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the item's lyrics.</returns>
    [HttpGet("Audio/{itemId}/Lyrics")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LyricDto>> GetLyrics([FromRoute, Required] Guid itemId)
    {
        var item = _libraryManager.GetItemById<Audio>(itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        var result = await _lyricManager.GetLyricsAsync(item, CancellationToken.None).ConfigureAwait(false);
        if (result is not null)
        {
            return Ok(result);
        }

        return NotFound();
    }

    /// <summary>
    /// Upload an external lyric file.
    /// </summary>
    /// <param name="itemId">The item the lyric belongs to.</param>
    /// <param name="fileName">Name of the file being uploaded.</param>
    /// <response code="200">Lyrics uploaded.</response>
    /// <response code="400">Error processing upload.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>The uploaded lyric.</returns>
    [HttpPost("Audio/{itemId}/Lyrics")]
    [Authorize(Policy = Policies.LyricManagement)]
    [AcceptsFile(MediaTypeNames.Text.Plain)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LyricDto>> UploadLyrics(
        [FromRoute, Required] Guid itemId,
        [FromQuery, Required] string fileName)
    {
        var item = _libraryManager.GetItemById<Audio>(itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        if (Request.ContentLength.GetValueOrDefault(0) == 0)
        {
            return BadRequest("No lyrics uploaded");
        }

        // Utilize Path.GetExtension as it provides extra path validation.
        var format = Path.GetExtension(fileName.AsSpan()).RightPart('.').ToString();
        if (string.IsNullOrEmpty(format))
        {
            return BadRequest("Extension is required on filename");
        }

        var stream = new MemoryStream();
        await using (stream.ConfigureAwait(false))
        {
            await Request.Body.CopyToAsync(stream).ConfigureAwait(false);
            var uploadedLyric = await _lyricManager.SaveLyricAsync(
                    item,
                    format,
                    stream)
                .ConfigureAwait(false);

            if (uploadedLyric is null)
            {
                return BadRequest();
            }

            _providerManager.QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);
            return Ok(uploadedLyric);
        }
    }

    /// <summary>
    /// Deletes an external lyric file.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <response code="204">Lyric deleted.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("Audio/{itemId}/Lyrics")]
    [Authorize(Policy = Policies.LyricManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteLyrics(
        [FromRoute, Required] Guid itemId)
    {
        var item = _libraryManager.GetItemById<Audio>(itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        await _lyricManager.DeleteLyricsAsync(item).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Search remote lyrics.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <response code="200">Lyrics retrieved.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>An array of <see cref="RemoteLyricInfo"/>.</returns>
    [HttpGet("Audio/{itemId}/RemoteSearch/Lyrics")]
    [Authorize(Policy = Policies.LyricManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RemoteLyricInfoDto>>> SearchRemoteLyrics([FromRoute, Required] Guid itemId)
    {
        var item = _libraryManager.GetItemById<Audio>(itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        var results = await _lyricManager.SearchLyricsAsync(item, false, CancellationToken.None).ConfigureAwait(false);
        return Ok(results);
    }

    /// <summary>
    /// Downloads a remote lyric.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="lyricId">The lyric id.</param>
    /// <response code="200">Lyric downloaded.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Audio/{itemId}/RemoteSearch/Lyrics/{lyricId}")]
    [Authorize(Policy = Policies.LyricManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LyricDto>> DownloadRemoteLyrics(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] string lyricId)
    {
        var item = _libraryManager.GetItemById<Audio>(itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        var downloadedLyrics = await _lyricManager.DownloadLyricsAsync(item, lyricId, CancellationToken.None).ConfigureAwait(false);
        if (downloadedLyrics is null)
        {
            return NotFound();
        }

        _providerManager.QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);
        return Ok(downloadedLyrics);
    }

    /// <summary>
    /// Gets the remote lyrics.
    /// </summary>
    /// <param name="lyricId">The remote provider item id.</param>
    /// <response code="200">File returned.</response>
    /// <response code="404">Lyric not found.</response>
    /// <returns>A <see cref="FileStreamResult"/> with the lyric file.</returns>
    [HttpGet("Providers/Lyrics/{lyricId}")]
    [Authorize(Policy = Policies.LyricManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LyricDto>> GetRemoteLyrics([FromRoute, Required] string lyricId)
    {
        var result = await _lyricManager.GetRemoteLyricsAsync(lyricId, CancellationToken.None).ConfigureAwait(false);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
