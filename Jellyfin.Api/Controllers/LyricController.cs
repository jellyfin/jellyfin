using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Models.LyricDtos;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Lyrics controller.
/// </summary>
[Route("")]
public class LyricController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILyricManager _lyricManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<LyricController> _logger;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LyricController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="lyricManager">Instance of the <see cref="ILyricManager"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{LyricController}"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public LyricController(
        ILibraryManager libraryManager,
        ILyricManager lyricManager,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILogger<LyricController> logger,
        IUserManager userManager)
    {
        _libraryManager = libraryManager;
        _lyricManager = lyricManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Deletes an external lyric file.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <response code="204">lyric deleted.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("Audio/{itemId}/Lyrics")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteLyrics(
        [FromRoute, Required] Guid itemId)
    {
        var audio = _libraryManager.GetItemById<Audio>(itemId);
        if (audio is null)
        {
            return NotFound();
        }

        await _lyricManager.DeleteLyricsAsync(audio).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Search remote lyrics.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <response code="200">lyrics retrieved.</response>
    /// <returns>An array of <see cref="RemoteLyricInfo"/>.</returns>
    [HttpGet("Items/{itemId}/RemoteSearch/Lyrics")]
    [Authorize(Policy = Policies.LyricManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RemoteLyricInfo>>> SearchRemotelyrics([FromRoute, Required] Guid itemId)
    {
        var audio = _libraryManager.GetItemById<Audio>(itemId);
        if (audio is null)
        {
            return NotFound();
        }

        return await _lyricManager.SearchLyricsAsync(audio, false, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a remote lyric.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="lyricId">The lyric id.</param>
    /// <response code="204">Lyric downloaded.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Items/{itemId}/RemoteSearch/Lyrics/{lyricId}")]
    [Authorize(Policy = Policies.LyricManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DownloadRemoteLyrics(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] string lyricId)
    {
        var audio = _libraryManager.GetItemById<Audio>(itemId);
        if (audio is null)
        {
            return NotFound();
        }

        try
        {
            await _lyricManager.DownloadLyricsAsync(audio, lyricId, CancellationToken.None)
                .ConfigureAwait(false);

            _providerManager.QueueRefresh(audio.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading lyrics");
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the remote lyrics.
    /// </summary>
    /// <param name="id">The item id.</param>
    /// <response code="200">File returned.</response>
    /// <returns>A <see cref="FileStreamResult"/> with the lyric file.</returns>
    [HttpGet("Providers/Lyrics/Lyrics/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces(MediaTypeNames.Application.Octet)]
    [ProducesFile("text/*")]
    public async Task<ActionResult> GetRemoteLyrics([FromRoute, Required] string id)
    {
        var result = await _lyricManager.GetRemoteLyricsAsync(id, CancellationToken.None).ConfigureAwait(false);
        return File(result.Stream, MimeTypes.GetMimeType("file.txt"));
    }

    /// <summary>
    /// Gets an item's lyrics.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="itemId">Item id.</param>
    /// <response code="200">Lyrics returned.</response>
    /// <response code="404">Something went wrong. No Lyrics will be returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the item's lyrics.</returns>
    [HttpGet("Users/{userId}/Items/{itemId}/Lyrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<LyricModel>> GetLyrics([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var audio = _libraryManager.GetItemById<Audio>(itemId);
        if (audio is null)
        {
            return NotFound();
        }

        // Check the item is visible for the user
        if (!audio.IsVisible(user))
        {
            return Unauthorized($"{user.Username} is not permitted to access item {audio.Name}.");
        }

        var result = await _lyricManager.GetLyricsAsync(audio, CancellationToken.None).ConfigureAwait(false);
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
    /// <param name="body">The request body.</param>
    /// <response code="204">Lyric uploaded.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Audio/{itemId}/Lyrics")]
    [Authorize(Policy = Policies.LyricManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UploadLyric(
        [FromRoute, Required] Guid itemId,
        [FromBody, Required] UploadLyricDto body)
    {
        var audio = _libraryManager.GetItemById<Audio>(itemId);
        if (audio is null)
        {
            return NotFound();
        }

        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(body.Data));
        using var transform = new FromBase64Transform();
        var stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read);
        await using (memoryStream.ConfigureAwait(false))
        await using (stream.ConfigureAwait(false))
        {
            await _lyricManager.UploadLyricAsync(
                audio,
                new LyricResponse
                {
                    Format = body.Format,
                    Stream = stream
                }).ConfigureAwait(false);
            _providerManager.QueueRefresh(audio.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);

            return NoContent();
        }
    }
}
