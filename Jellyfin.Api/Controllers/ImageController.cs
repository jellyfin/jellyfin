using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Image controller.
    /// </summary>
    public class ImageController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly IFileSystem _fileSystem;
        private readonly IAuthorizationContext _authContext;
        private readonly ILogger<ImageController> _logger;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="imageProcessor">Instance of the <see cref="IImageProcessor"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ImageController}"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public ImageController(
            IUserManager userManager,
            ILibraryManager libraryManager,
            IProviderManager providerManager,
            IImageProcessor imageProcessor,
            IFileSystem fileSystem,
            IAuthorizationContext authContext,
            ILogger<ImageController> logger,
            IServerConfigurationManager serverConfigurationManager)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _imageProcessor = imageProcessor;
            _fileSystem = fileSystem;
            _authContext = authContext;
            _logger = logger;
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <summary>
        /// Sets the user image.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="imageType">(Unused) Image type.</param>
        /// <param name="index">(Unused) Image index.</param>
        /// <response code="204">Image updated.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("/Users/{userId}/Images/{imageType}")]
        [HttpPost("/Users/{userId}/Images/{imageType}/{index}")]
        public async Task<ActionResult> PostUserImage(
            [FromRoute] Guid userId,
            [FromRoute] ImageType imageType,
            [FromRoute] int? index)
        {
            // TODO AssertCanUpdateUser(_authContext, _userManager, id, true);

            var user = _userManager.GetUserById(userId);
            await using var memoryStream = await GetMemoryStream(Request.Body).ConfigureAwait(false);

            // Handle image/png; charset=utf-8
            var mimeType = Request.ContentType.Split(';').FirstOrDefault();
            var userDataPath = Path.Combine(_serverConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath, user.Username);
            user.ProfileImage = new Data.Entities.ImageInfo(Path.Combine(userDataPath, "profile" + MimeTypes.ToExtension(mimeType)));

            await _providerManager
                .SaveImage(user, memoryStream, mimeType, user.ProfileImage.Path)
                .ConfigureAwait(false);
            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Delete the user's image.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="imageType">(Unused) Image type.</param>
        /// <param name="index">(Unused) Image index.</param>
        /// <response code="204">Image deleted.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpDelete("/Users/{userId}/Images/{itemType}")]
        [HttpDelete("/Users/{userId}/Images/{itemType}/{index}")]
        public ActionResult DeleteUserImage(
            [FromRoute] Guid userId,
            [FromRoute] ImageType imageType,
            [FromRoute] int? index)
        {
            // TODO AssertCanUpdateUser(_authContext, _userManager, userId, true);

            var user = _userManager.GetUserById(userId);
            try
            {
                System.IO.File.Delete(user.ProfileImage.Path);
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Error deleting user profile image:");
            }

            _userManager.ClearProfileImage(user);
            return NoContent();
        }

        private static async Task<MemoryStream> GetMemoryStream(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var text = await reader.ReadToEndAsync().ConfigureAwait(false);

            var bytes = Convert.FromBase64String(text);
            return new MemoryStream(bytes)
            {
                Position = 0
            };
        }
    }
}
