using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    [Route("")]
    public class MediaUploadController : BaseJellyfinApiController
    {
        private readonly ILogger<MediaUploadController> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public MediaUploadController(
            ILogger<MediaUploadController> logger,
            ILibraryManager libraryManager,
            IUserManager userManager)
            : base()
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        [HttpPost("Media/Upload")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UploadMediaFile(
            [Required] IFormFile file,
            [FromQuery] Guid? libraryId,
            [FromQuery] string? collectionType)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload request received with no file or empty file.");
                return BadRequest("File is required and cannot be empty.");
            }

            _logger.LogInformation(
                "Received file upload request for {FileName}. LibraryId: {LibraryId}, CollectionType: {CollectionType}",
                file.FileName,
                libraryId?.ToString() ?? "null",
                collectionType ?? "null");

            try
            {
                var success = await _libraryManager.AddUploadedMediaFile(file, libraryId, collectionType);

                if (success)
                {
                    _logger.LogInformation("File {FileName} processed and saved successfully.", file.FileName);
                    return NoContent(); // Or Ok("File uploaded successfully.");
                }
                else
                {
                    _logger.LogError("Failed to process or save file {FileName}. Check LibraryManager logs for details.", file.FileName);
                    // Consider returning a more specific error based on LibraryManager's failure reason if available
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to save file {file.FileName}. The server encountered an internal error.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while uploading file {FileName}.", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during file upload.");
            }
        }
    }
}
