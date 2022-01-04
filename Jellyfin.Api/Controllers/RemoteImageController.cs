using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Remote Images Controller.
    /// </summary>
    [Route("")]
    public class RemoteImageController : BaseJellyfinApiController
    {
        private const string RemoteImagesRoute = "RemoteImages";
        private const string FetchRoute = "Fetch";

        private readonly IProviderManager _providerManager;
        private readonly IServerApplicationPaths _applicationPaths;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteImageController"/> class.
        /// </summary>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="applicationPaths">Instance of the <see cref="IServerApplicationPaths"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public RemoteImageController(
            IProviderManager providerManager,
            IServerApplicationPaths applicationPaths,
            IHttpClientFactory httpClientFactory,
            ILibraryManager libraryManager)
        {
            _providerManager = providerManager;
            _applicationPaths = applicationPaths;
            _httpClientFactory = httpClientFactory;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets available remote images for an item.
        /// </summary>
        /// <param name="itemId">Item Id.</param>
        /// <param name="type">The image type.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="providerName">Optional. The image provider to use.</param>
        /// <param name="includeAllLanguages">Optional. Include all languages.</param>
        /// <response code="200">Remote Images returned.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>Remote Image Result.</returns>
        [HttpGet($"Items/{{itemId}}/{RemoteImagesRoute}")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RemoteImageResult>> GetRemoteImages(
            [FromRoute, Required] Guid itemId,
            [FromQuery] ImageType? type,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string? providerName,
            [FromQuery] bool includeAllLanguages = false)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            var images = await _providerManager.GetAvailableRemoteImages(
                    item,
                    new RemoteImageQuery(providerName ?? string.Empty)
                    {
                        IncludeAllLanguages = includeAllLanguages,
                        IncludeDisabledProviders = true,
                        ImageType = type
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);

            var fetchUrlPrefix = GetFetchUrlPrefix();
            foreach (var image in images)
            {
                if (!string.IsNullOrWhiteSpace(image.Url))
                {
                    image.Url = fetchUrlPrefix + WebUtility.UrlEncode(image.Url);
                }

                if (!string.IsNullOrWhiteSpace(image.ThumbnailUrl))
                {
                    image.ThumbnailUrl = fetchUrlPrefix + WebUtility.UrlEncode(image.ThumbnailUrl);
                }
            }

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
            return result;
        }

        /// <summary>
        /// Gets available remote image providers for an item.
        /// </summary>
        /// <param name="itemId">Item Id.</param>
        /// <response code="200">Returned remote image providers.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>List of remote image providers.</returns>
        [HttpGet($"Items/{{itemId}}/{RemoteImagesRoute}/Providers")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<ImageProviderInfo>> GetRemoteImageProviders([FromRoute, Required] Guid itemId)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(_providerManager.GetRemoteImageProviderInfo(item));
        }

        /// <summary>
        /// Downloads a remote image for an item.
        /// </summary>
        /// <param name="itemId">Item Id.</param>
        /// <param name="type">The image type.</param>
        /// <param name="imageUrl">The image url.</param>
        /// <response code="204">Remote image downloaded.</response>
        /// <response code="404">Remote image not found.</response>
        /// <returns>Download status.</returns>
        /// <remarks>This endpoint downloads the remote image to the library.</remarks>
        [HttpPost($"Items/{{itemId}}/{RemoteImagesRoute}/Download")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DownloadRemoteImage(
            [FromRoute, Required] Guid itemId,
            [FromQuery, Required] ImageType type,
            [FromQuery] string? imageUrl)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            var fetchUrlPrefix = GetFetchUrlPrefix();
            if (imageUrl!.StartsWith(fetchUrlPrefix))
            {
                imageUrl = WebUtility.UrlDecode(imageUrl.Substring(fetchUrlPrefix.Length));
            }

            await _providerManager.SaveImage(item, imageUrl, type, null, CancellationToken.None)
                .ConfigureAwait(false);

            await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Fetches a remote image for an item.
        /// </summary>
        /// <param name="itemId">Item Id.</param>
        /// <param name="imageUrl">The image url.</param>
        /// <response code="200">Remote image fetched.</response>
        /// <response code="404">Remote image not found.</response>
        /// <returns>The fetched image.</returns>
        /// <remarks>This endpoint fetches and returns the remote image to the client without updating the library.</remarks>
        [HttpGet($"Items/{{itemId}}/{RemoteImagesRoute}/{FetchRoute}")]
        [HttpHead($"Items/{{itemId}}/{RemoteImagesRoute}/{FetchRoute}", Name = "HeadRemoteImage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesImageFile]
        public async Task<ActionResult> FetchRemoteImage(
            [FromRoute, Required] Guid itemId,
            [FromQuery] string? imageUrl)
        {
            // Fetching remote image does not depend on the library item, but this check is still useful to prevent
            // abuse. This endpoint does not require authorization and cannot since image requests in web clients
            // are initiated by the browsers, not the JavaScript XHR.
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            var response = await httpClient.GetAsync(imageUrl).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType ?? new MediaTypeHeaderValue("image/jpeg");
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return new FileStreamResult(stream, contentType!.ToString());
        }

        /// <summary>
        /// Gets the item-aware url prefix for the fetch endpoint.
        /// </summary>
        /// <returns>The item-aware url prefix for the fetch endpoint.</returns>
        private string GetFetchUrlPrefix()
        {
            var path = UriHelper.BuildRelative(HttpContext.Request.PathBase, HttpContext.Request.Path);
            var remoteImagesRouteIndex = path.LastIndexOf(RemoteImagesRoute);
            if (remoteImagesRouteIndex < 0)
            {
                throw new ResourceNotFoundException(RemoteImagesRoute);
            }

            path = path.Substring(0, remoteImagesRouteIndex + RemoteImagesRoute.Length);
            return $"{path}/{FetchRoute}?imageUrl=";
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
    }
}
