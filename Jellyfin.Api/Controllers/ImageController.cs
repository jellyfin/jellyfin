using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

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
        [HttpPost("/Users/{userId}/Images/{imageType}/{index?}")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "imageType", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
        public async Task<ActionResult> PostUserImage(
            [FromRoute] Guid userId,
            [FromRoute] ImageType imageType,
            [FromRoute] int? index = null)
        {
            if (!RequestHelpers.AssertCanUpdateUser(_authContext, HttpContext.Request, userId, true))
            {
                return Forbid("User is not allowed to update the image.");
            }

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
        [HttpDelete("/Users/{userId}/Images/{itemType}/{index?}")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "imageType", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult DeleteUserImage(
            [FromRoute] Guid userId,
            [FromRoute] ImageType imageType,
            [FromRoute] int? index = null)
        {
            if (!RequestHelpers.AssertCanUpdateUser(_authContext, HttpContext.Request, userId, true))
            {
                return Forbid("User is not allowed to delete the image.");
            }

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

        /// <summary>
        /// Delete an item's image.
        /// </summary>
        /// <param name="itemId">Item id.</param>
        /// <param name="imageType">Image type.</param>
        /// <param name="imageIndex">The image index.</param>
        /// <response code="204">Image deleted.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
        [HttpDelete("/Items/{itemId}/Images/{imageType}")]
        [HttpDelete("/Items/{itemId}/Images/{imageType}/{imageIndex?}")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteItemImage(
            [FromRoute] Guid itemId,
            [FromRoute] ImageType imageType,
            [FromRoute] int? imageIndex = null)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            item.DeleteImage(imageType, imageIndex ?? 0);
            return NoContent();
        }

        /// <summary>
        /// Set item image.
        /// </summary>
        /// <param name="itemId">Item id.</param>
        /// <param name="imageType">Image type.</param>
        /// <param name="imageIndex">(Unused) Image index.</param>
        /// <response code="204">Image saved.</response>
        /// <response code="400">Item not found.</response>
        /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
        [HttpPost("/Items/{itemId}/Images/{imageType}")]
        [HttpPost("/Items/{itemId}/Images/{imageType}/{imageIndex?}")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
        public async Task<ActionResult> SetItemImage(
            [FromRoute] Guid itemId,
            [FromRoute] ImageType imageType,
            [FromRoute] int? imageIndex = null)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            // Handle image/png; charset=utf-8
            var mimeType = Request.ContentType.Split(';').FirstOrDefault();
            await _providerManager.SaveImage(item, Request.Body, mimeType, imageType, null, CancellationToken.None).ConfigureAwait(false);
            item.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);

            return NoContent();
        }

        /// <summary>
        /// Updates the index for an item image.
        /// </summary>
        /// <param name="itemId">Item id.</param>
        /// <param name="imageType">Image type.</param>
        /// <param name="imageIndex">Old image index.</param>
        /// <param name="newIndex">New image index.</param>
        /// <response code="204">Image index updated.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
        [HttpPost("/Items/{itemId}/Images/{imageType}/{imageIndex}/Index")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateItemImageIndex(
            [FromRoute] Guid itemId,
            [FromRoute] ImageType imageType,
            [FromRoute] int imageIndex,
            [FromQuery] int newIndex)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            item.SwapImages(imageType, imageIndex, newIndex);
            return NoContent();
        }

        /// <summary>
        /// Get item image infos.
        /// </summary>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Item images returned.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>The list of image infos on success, or <see cref="NotFoundResult"/> if item not found.</returns>
        [HttpGet("/Items/{itemId}/Images")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<ImageInfo>> GetItemImageInfos([FromRoute] Guid itemId)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            var list = new List<ImageInfo>();
            var itemImages = item.ImageInfos;

            if (itemImages.Length == 0)
            {
                // short-circuit
                return list;
            }

            _libraryManager.UpdateImages(item); // this makes sure dimensions and hashes are correct

            foreach (var image in itemImages)
            {
                if (!item.AllowsMultipleImages(image.Type))
                {
                    var info = GetImageInfo(item, image, null);

                    if (info != null)
                    {
                        list.Add(info);
                    }
                }
            }

            foreach (var imageType in itemImages.Select(i => i.Type).Distinct().Where(item.AllowsMultipleImages))
            {
                var index = 0;

                // Prevent implicitly captured closure
                var currentImageType = imageType;

                foreach (var image in itemImages.Where(i => i.Type == currentImageType))
                {
                    var info = GetImageInfo(item, image, index);

                    if (info != null)
                    {
                        list.Add(info);
                    }

                    index++;
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the item's image.
        /// </summary>
        /// <param name="itemId">Item id.</param>
        /// <param name="imageType">Image type.</param>
        /// <param name="maxWidth">The maximum image width to return.</param>
        /// <param name="maxHeight">The maximum image height to return.</param>
        /// <param name="width">The fixed image width to return.</param>
        /// <param name="height">The fixed image height to return.</param>
        /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
        /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
        /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
        /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
        /// <param name="addPlayedIndicator">Optional. Add a played indicator.</param>
        /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
        /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
        /// <param name="blur">Optional. Blur image.</param>
        /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
        /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
        /// <param name="imageIndex">Image index.</param>
        /// <param name="enableImageEnhancers">Enable or disable image enhancers such as cover art.</param>
        /// <response code="200">Image stream returned.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>
        /// A <see cref="FileStreamResult"/> containing the file stream on success,
        /// or a <see cref="NotFoundResult"/> if item not found.
        /// </returns>
        [HttpGet("/Items/{itemId}/Images/{imageType}")]
        [HttpHead("/Items/{itemId}/Images/{imageType}")]
        [HttpGet("/Items/{itemId}/Images/{imageType}/{imageIndex?}")]
        [HttpHead("/Items/{itemId}/Images/{imageType}/{imageIndex?}")]
        public async Task<ActionResult> GetItemImage(
            [FromRoute] Guid itemId,
            [FromRoute] ImageType imageType,
            [FromRoute] int? maxWidth,
            [FromRoute] int? maxHeight,
            [FromQuery] int? width,
            [FromQuery] int? height,
            [FromQuery] int? quality,
            [FromQuery] string tag,
            [FromQuery] bool? cropWhitespace,
            [FromQuery] string format,
            [FromQuery] bool addPlayedIndicator,
            [FromQuery] double? percentPlayed,
            [FromQuery] int? unplayedCount,
            [FromQuery] int? blur,
            [FromQuery] string backgroundColor,
            [FromQuery] string foregroundLayer,
            [FromRoute] int? imageIndex = null,
            [FromQuery] bool enableImageEnhancers = true)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            return await GetImageInternal(
                    itemId,
                    imageType,
                    imageIndex,
                    tag,
                    format,
                    maxWidth,
                    maxHeight,
                    percentPlayed,
                    unplayedCount,
                    width,
                    height,
                    quality,
                    cropWhitespace,
                    addPlayedIndicator,
                    blur,
                    backgroundColor,
                    foregroundLayer,
                    enableImageEnhancers,
                    item,
                    Request.Method.Equals(HttpMethods.Head, StringComparison.OrdinalIgnoreCase))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the item's image.
        /// </summary>
        /// <param name="itemId">Item id.</param>
        /// <param name="imageType">Image type.</param>
        /// <param name="imageIndex">Image index.</param>
        /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
        /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
        /// <param name="maxWidth">The maximum image width to return.</param>
        /// <param name="maxHeight">The maximum image height to return.</param>
        /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
        /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
        /// <param name="width">The fixed image width to return.</param>
        /// <param name="height">The fixed image height to return.</param>
        /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
        /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
        /// <param name="addPlayedIndicator">Optional. Add a played indicator.</param>
        /// <param name="blur">Optional. Blur image.</param>
        /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
        /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
        /// <param name="enableImageEnhancers">Enable or disable image enhancers such as cover art.</param>
        /// <response code="200">Image stream returned.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>
        /// A <see cref="FileStreamResult"/> containing the file stream on success,
        /// or a <see cref="NotFoundResult"/> if item not found.
        /// </returns>
        [HttpGet("/Items/{itemId}/Images/{imageType}/{imageIndex}/{tag}/{format}/{maxWidth}/{maxHeight}/{percentPlayed}/{unplayedCount}")]
        [HttpHead("/Items/{itemId}/Images/{imageType}/{imageIndex}/{tag}/{format}/{maxWidth}/{maxHeight}/{percentPlayed}/{unplayedCount}")]
        public ActionResult<object> GetItemImage(
            [FromRoute] Guid itemId,
            [FromRoute] ImageType imageType,
            [FromRoute] int? imageIndex,
            [FromRoute] string tag,
            [FromRoute] string format,
            [FromRoute] int? maxWidth,
            [FromRoute] int? maxHeight,
            [FromRoute] double? percentPlayed,
            [FromRoute] int? unplayedCount,
            [FromQuery] int? width,
            [FromQuery] int? height,
            [FromQuery] int? quality,
            [FromQuery] bool? cropWhitespace,
            [FromQuery] bool addPlayedIndicator,
            [FromQuery] int? blur,
            [FromQuery] string backgroundColor,
            [FromQuery] string foregroundLayer,
            [FromQuery] bool enableImageEnhancers = true)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            return GetImageInternal(
                itemId,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                cropWhitespace,
                addPlayedIndicator,
                blur,
                backgroundColor,
                foregroundLayer,
                enableImageEnhancers,
                item,
                Request.Method.Equals(HttpMethods.Head, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<MemoryStream> GetMemoryStream(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var text = await reader.ReadToEndAsync().ConfigureAwait(false);

            var bytes = Convert.FromBase64String(text);
            return new MemoryStream(bytes) {Position = 0};
        }

        private ImageInfo? GetImageInfo(BaseItem item, ItemImageInfo info, int? imageIndex)
        {
            int? width = null;
            int? height = null;
            string? blurhash = null;
            long length = 0;

            try
            {
                if (info.IsLocalFile)
                {
                    var fileInfo = _fileSystem.GetFileInfo(info.Path);
                    length = fileInfo.Length;

                    blurhash = info.BlurHash;
                    width = info.Width;
                    height = info.Height;

                    if (width <= 0 || height <= 0)
                    {
                        width = null;
                        height = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image information for {Item}", item.Name);
            }

            try
            {
                return new ImageInfo
                {
                    Path = info.Path,
                    ImageIndex = imageIndex,
                    ImageType = info.Type,
                    ImageTag = _imageProcessor.GetImageCacheTag(item, info),
                    Size = length,
                    BlurHash = blurhash,
                    Width = width,
                    Height = height
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image information for {Path}", info.Path);

                return null;
            }
        }

        private async Task<ActionResult> GetImageInternal(
            Guid itemId,
            ImageType imageType,
            int? imageIndex,
            string tag,
            string format,
            int? maxWidth,
            int? maxHeight,
            double? percentPlayed,
            int? unplayedCount,
            int? width,
            int? height,
            int? quality,
            bool? cropWhitespace,
            bool addPlayedIndicator,
            int? blur,
            string backgroundColor,
            string foregroundLayer,
            bool enableImageEnhancers,
            BaseItem item,
            bool isHeadRequest)
        {
            if (percentPlayed.HasValue)
            {
                if (percentPlayed.Value <= 0)
                {
                    percentPlayed = null;
                }
                else if (percentPlayed.Value >= 100)
                {
                    percentPlayed = null;
                    addPlayedIndicator = true;
                }
            }

            if (percentPlayed.HasValue)
            {
                unplayedCount = null;
            }

            if (unplayedCount.HasValue
                && unplayedCount.Value <= 0)
            {
                unplayedCount = null;
            }

            var imageInfo = item.GetImageInfo(imageType, imageIndex ?? 0);
            if (imageInfo == null)
            {
                return NotFound(string.Format(NumberFormatInfo.InvariantInfo, "{0} does not have an image of type {1}", item.Name, imageType));
            }

            if (!cropWhitespace.HasValue)
            {
                cropWhitespace = imageType == ImageType.Logo || imageType == ImageType.Art;
            }

            var outputFormats = GetOutputFormats(format);

            TimeSpan? cacheDuration = null;

            if (!string.IsNullOrEmpty(tag))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var responseHeaders = new Dictionary<string, string> {{"transferMode.dlna.org", "Interactive"}, {"realTimeInfo.dlna.org", "DLNA.ORG_TLAG=*"}};

            return await GetImageResult(
                item,
                itemId,
                imageIndex,
                height,
                maxHeight,
                maxWidth,
                quality,
                width,
                addPlayedIndicator,
                percentPlayed,
                unplayedCount,
                blur,
                backgroundColor,
                foregroundLayer,
                imageInfo,
                cropWhitespace.Value,
                outputFormats,
                cacheDuration,
                responseHeaders,
                isHeadRequest).ConfigureAwait(false);
        }

        private ImageFormat[] GetOutputFormats(string format)
        {
            if (!string.IsNullOrWhiteSpace(format)
                && Enum.TryParse(format, true, out ImageFormat parsedFormat))
            {
                return new[] {parsedFormat};
            }

            return GetClientSupportedFormats();
        }

        private ImageFormat[] GetClientSupportedFormats()
        {
            var acceptTypes = Request.Headers[HeaderNames.Accept];
            var supportedFormats = new List<string>();
            if (acceptTypes.Count > 0)
            {
                foreach (var type in acceptTypes)
                {
                    int index = type.IndexOf(';', StringComparison.Ordinal);
                    if (index != -1)
                    {
                        supportedFormats.Add(type.Substring(0, index));
                    }
                }
            }

            var acceptParam = Request.Query[HeaderNames.Accept];

            var supportsWebP = SupportsFormat(supportedFormats, acceptParam, "webp", false);

            if (!supportsWebP)
            {
                var userAgent = Request.Headers[HeaderNames.UserAgent].ToString();
                if (userAgent.IndexOf("crosswalk", StringComparison.OrdinalIgnoreCase) != -1 &&
                    userAgent.IndexOf("android", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    supportsWebP = true;
                }
            }

            var formats = new List<ImageFormat>(4);

            if (supportsWebP)
            {
                formats.Add(ImageFormat.Webp);
            }

            formats.Add(ImageFormat.Jpg);
            formats.Add(ImageFormat.Png);

            if (SupportsFormat(supportedFormats, acceptParam, "gif", true))
            {
                formats.Add(ImageFormat.Gif);
            }

            return formats.ToArray();
        }

        private bool SupportsFormat(IReadOnlyCollection<string> requestAcceptTypes, string acceptParam, string format, bool acceptAll)
        {
            var mimeType = "image/" + format;

            if (requestAcceptTypes.Contains(mimeType))
            {
                return true;
            }

            if (acceptAll && requestAcceptTypes.Contains("*/*"))
            {
                return true;
            }

            return string.Equals(acceptParam, format, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<ActionResult> GetImageResult(
            BaseItem item,
            Guid itemId,
            int? index,
            int? height,
            int? maxHeight,
            int? maxWidth,
            int? quality,
            int? width,
            bool addPlayedIndicator,
            double? percentPlayed,
            int? unplayedCount,
            int? blur,
            string backgroundColor,
            string foregroundLayer,
            ItemImageInfo imageInfo,
            bool cropWhitespace,
            IReadOnlyCollection<ImageFormat> supportedFormats,
            TimeSpan? cacheDuration,
            IDictionary<string, string> headers,
            bool isHeadRequest)
        {
            if (!imageInfo.IsLocalFile)
            {
                imageInfo = await _libraryManager.ConvertImageToLocal(item, imageInfo, index ?? 0).ConfigureAwait(false);
            }

            var options = new ImageProcessingOptions
            {
                CropWhiteSpace = cropWhitespace,
                Height = height,
                ImageIndex = index ?? 0,
                Image = imageInfo,
                Item = item,
                ItemId = itemId,
                MaxHeight = maxHeight,
                MaxWidth = maxWidth,
                Quality = quality ?? 100,
                Width = width,
                AddPlayedIndicator = addPlayedIndicator,
                PercentPlayed = percentPlayed ?? 0,
                UnplayedCount = unplayedCount,
                Blur = blur,
                BackgroundColor = backgroundColor,
                ForegroundLayer = foregroundLayer,
                SupportedOutputFormats = supportedFormats
            };

            var imageResult = await _imageProcessor.ProcessImage(options).ConfigureAwait(false);

            headers[HeaderNames.Vary] = HeaderNames.Accept;
            /*
             // TODO
            return _resultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                CacheDuration = cacheDuration,
                ResponseHeaders = headers,
                ContentType = imageResult.Item2,
                DateLastModified = imageResult.Item3,
                IsHeadRequest = isHeadRequest,
                Path = imageResult.Item1,
                FileShare = FileShare.Read
            });
            */
            return NoContent();
        }
    }
}
