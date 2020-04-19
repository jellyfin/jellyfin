#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Attachments controller.
    /// </summary>
    [Route("Videos")]
    [Authenticated]
    public class AttachmentsController : Controller
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IAttachmentExtractor _attachmentExtractor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentsController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="attachmentExtractor">Instance of the <see cref="IAttachmentExtractor"/> interface.</param>
        public AttachmentsController(
            ILibraryManager libraryManager,
            IAttachmentExtractor attachmentExtractor)
        {
            _libraryManager = libraryManager;
            _attachmentExtractor = attachmentExtractor;
        }

        /// <summary>
        /// Get video attachment.
        /// </summary>
        /// <param name="videoId">Video ID.</param>
        /// <param name="mediaSourceId">Media Source ID.</param>
        /// <param name="index">Attachment Index.</param>
        /// <returns>Attachment.</returns>
        [HttpGet("{VideoID}/{MediaSourceID}/Attachments/{Index}")]
        [Produces("application/octet-stream")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAttachment(
            [FromRoute] Guid videoId,
            [FromRoute] string mediaSourceId,
            [FromRoute] int index)
        {
            try
            {
                var item = _libraryManager.GetItemById(videoId);
                if (item == null)
                {
                    return NotFound();
                }

                var (attachment, stream) = await _attachmentExtractor.GetAttachment(
                        item,
                        mediaSourceId,
                        index,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var contentType = "application/octet-stream";
                if (string.IsNullOrWhiteSpace(attachment.MimeType))
                {
                    contentType = attachment.MimeType;
                }

                return new FileStreamResult(stream, contentType);
            }
            catch (ResourceNotFoundException e)
            {
                return StatusCode(StatusCodes.Status404NotFound, e.Message);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
