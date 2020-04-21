#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers.Images
{
    /// <summary>
    /// Remote Images Controller.
    /// </summary>
    [Route("Images")]
    [Authenticated]
    public class RemoteImageController : BaseJellyfinApiController
    {
        private readonly IProviderManager _providerManager;
        private readonly IServerApplicationPaths _applicationPaths;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteImageController"/> class.
        /// </summary>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="applicationPaths">Instance of the <see cref="IServerApplicationPaths"/> interface.</param>
        /// <param name="httpClient">Instance of the <see cref="IHttpClient"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public RemoteImageController(
            IProviderManager providerManager,
            IServerApplicationPaths applicationPaths,
            IHttpClient httpClient,
            ILibraryManager libraryManager)
        {
            _providerManager = providerManager;
            _applicationPaths = applicationPaths;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets available remote images for an item.
        /// </summary>
        /// <param name="id">Item Id.</param>
        /// <param name="type">The image type.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="providerName">Optional. The image provider to use.</param>
        /// <param name="includeAllLanguages">Optinal. Include all languages.</param>
        /// <returns>Remote Image Result.</returns>
        [HttpGet("{Id}/RemoteImages")]
        [ProducesResponseType(typeof(RemoteImageResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRemoteImages(
            [FromRoute] string id,
            [FromQuery] ImageType? type,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string providerName,
            [FromQuery] bool includeAllLanguages)
        {
            var item = _libraryManager.GetItemById(id);
            if (item == null)
            {
                return NotFound();
            }

            var images = await _providerManager.GetAvailableRemoteImages(
                    item,
                    new RemoteImageQuery
                    {
                        ProviderName = providerName,
                        IncludeAllLanguages = includeAllLanguages,
                        IncludeDisabledProviders = true,
                        ImageType = type
                    }, CancellationToken.None)
                .ConfigureAwait(false);

            var imageArray = images.ToArray();
            var allProviders = _providerManager.GetRemoteImageProviderInfo(item);
            if (type.HasValue)
            {
                allProviders = allProviders.Where(o => o.SupportedImages.Contains(type.Value));
            }

            var result = new RemoteImageResult
            {
                TotalRecordCount = imageArray.Length,
                Providers = allProviders.Select(o => o.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };

            if (startIndex.HasValue)
            {
                imageArray = imageArray.Skip(startIndex.Value).ToArray();
            }

            if (limit.HasValue)
            {
                imageArray = imageArray.Take(limit.Value).ToArray();
            }

            result.Images = imageArray;
            return Ok(result);
        }

        /// <summary>
        /// Gets available remote image providers for an item.
        /// </summary>
        /// <param name="id">Item Id.</param>
        /// <returns>List of providers.</returns>
        [HttpGet("{Id}/RemoteImages/Providers")]
        [ProducesResponseType(typeof(ImageProviderInfo[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetRemoteImageProviders([FromRoute] string id)
        {
            var item = _libraryManager.GetItemById(id);
            if (item == null)
            {
                return NotFound();
            }

            var providers = _providerManager.GetRemoteImageProviderInfo(item);
            return Ok(providers);
        }

        /// <summary>
        /// Gets a remote image.
        /// </summary>
        /// <param name="imageUrl">The image url.</param>
        /// <returns>Image Stream.</returns>
        [HttpGet("Remote")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRemoteImage([FromQuery, BindRequired] string imageUrl)
        {
            var urlHash = imageUrl.GetMD5();
            var pointerCachePath = GetFullCachePath(urlHash.ToString());

            string? contentPath = null;
            bool hasFile = false;

            try
            {
                contentPath = await System.IO.File.ReadAllTextAsync(pointerCachePath).ConfigureAwait(false);
                if (System.IO.File.Exists(contentPath))
                {
                    hasFile = true;
                }
            }
            catch (FileNotFoundException)
            {
                // Means the file isn't cached yet
            }
            catch (IOException)
            {
                // Means the file isn't cached yet
            }

            if (!hasFile)
            {
                await DownloadImage(imageUrl, urlHash, pointerCachePath).ConfigureAwait(false);
                contentPath = await System.IO.File.ReadAllTextAsync(pointerCachePath).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(contentPath))
            {
                return NotFound();
            }

            var contentType = MimeTypes.GetMimeType(contentPath);
            return new FileStreamResult(System.IO.File.OpenRead(contentPath), contentType);
        }

        /// <summary>
        /// Downloads a remote image for an item.
        /// </summary>
        /// <param name="id">Item Id.</param>
        /// <param name="type">The image type.</param>
        /// <param name="imageUrl">The image url.</param>
        /// <returns>Status.</returns>
        [HttpPost("{Id}/RemoteImages/Download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadRemoteImage(
            [FromRoute] string id,
            [FromQuery, BindRequired] ImageType type,
            [FromQuery] string imageUrl)
        {
            var item = _libraryManager.GetItemById(id);
            if (item == null)
            {
                return NotFound();
            }

            await _providerManager.SaveImage(item, imageUrl, type, null, CancellationToken.None)
                .ConfigureAwait(false);

            item.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);
            return Ok();
        }

        /// <summary>
        /// Gets the full cache path.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        private string GetFullCachePath(string filename)
        {
            return Path.Combine(_applicationPaths.CachePath, "remote-images", filename.Substring(0, 1), filename);
        }

        /// <summary>
        /// Downloads the image.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="urlHash">The URL hash.</param>
        /// <param name="pointerCachePath">The pointer cache path.</param>
        /// <returns>Task.</returns>
        private async Task DownloadImage(string url, Guid urlHash, string pointerCachePath)
        {
            using var result = await _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                BufferContent = false
            }).ConfigureAwait(false);
            var ext = result.ContentType.Split('/').Last();

            var fullCachePath = GetFullCachePath(urlHash + "." + ext);

            Directory.CreateDirectory(Path.GetDirectoryName(fullCachePath));
            using (var stream = result.Content)
            {
                using var filestream = new FileStream(fullCachePath, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true);
                await stream.CopyToAsync(filestream).ConfigureAwait(false);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(pointerCachePath));
            await System.IO.File.WriteAllTextAsync(pointerCachePath, fullCachePath, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
