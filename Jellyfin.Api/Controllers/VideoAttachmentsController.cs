using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Attachments controller.
/// </summary>
[Route("Videos")]
[Tags("Video")]
public class VideoAttachmentsController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly IAttachmentExtractor _attachmentExtractor;
    private readonly UploadHelper _uploadHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoAttachmentsController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="attachmentExtractor">Instance of the <see cref="IAttachmentExtractor"/> interface.</param>
    /// <param name="uploadHelper">The <see cref="UploadHelper"/> instance.</param>
    public VideoAttachmentsController(
        ILibraryManager libraryManager,
        IAttachmentExtractor attachmentExtractor,
        UploadHelper uploadHelper)
    {
        _libraryManager = libraryManager;
        _attachmentExtractor = attachmentExtractor;
        _uploadHelper = uploadHelper;
    }

    /// <summary>
    /// Get video attachment.
    /// </summary>
    /// <param name="videoId">Video ID.</param>
    /// <param name="mediaSourceId">Media Source ID.</param>
    /// <param name="index">Attachment Index.</param>
    /// <response code="200">Attachment retrieved.</response>
    /// <response code="404">Video or attachment not found.</response>
    /// <returns>An <see cref="FileStreamResult"/> containing the attachment stream on success, or a <see cref="NotFoundResult"/> if the attachment could not be found.</returns>
    [HttpGet("{videoId}/{mediaSourceId}/Attachments/{index}")]
    [ProducesFile(MediaTypeNames.Application.Octet)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetAttachment(
        [FromRoute, Required] Guid videoId,
        [FromRoute, Required] string mediaSourceId,
        [FromRoute, Required] int index)
    {
        try
        {
            var item = _libraryManager.GetItemById<BaseItem>(videoId, User.GetUserId());
            if (item is null)
            {
                return NotFound();
            }

            var (_, stream) = await _attachmentExtractor.GetAttachment(
                    item,
                    mediaSourceId,
                    index,
                    CancellationToken.None)
                .ConfigureAwait(false);

            // The MIME type the media file declares for the attachment is chosen by whoever created that
            // file, so serving it would let an attachment claim to be e.g. HTML and run scripts on our origin.
            // Detect the format from the content instead and ignore what the attachment claims to be.
            Response.Headers.ContentDisposition = "attachment";
            Response.Headers.XContentTypeOptions = "nosniff";

            return new FileStreamResult(stream, _uploadHelper.GetAttachmentMimeType(stream));
        }
        catch (ResourceNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
