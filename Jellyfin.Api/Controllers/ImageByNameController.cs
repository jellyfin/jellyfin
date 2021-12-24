using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    ///     Images By Name Controller.
    /// </summary>
    [Route("Images")]
    public class ImageByNameController : BaseJellyfinApiController
    {
        private readonly IServerApplicationPaths _applicationPaths;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImageByNameController" /> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager" /> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem" /> interface.</param>
        public ImageByNameController(
            IServerConfigurationManager serverConfigurationManager,
            IFileSystem fileSystem)
        {
            _applicationPaths = serverConfigurationManager.ApplicationPaths;
            _fileSystem = fileSystem;
        }

        /// <summary>
        ///     Get all general images.
        /// </summary>
        /// <response code="200">Retrieved list of images.</response>
        /// <returns>An <see cref="OkResult"/> containing the list of images.</returns>
        [HttpGet("General")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<ImageByNameInfo>> GetGeneralImages()
        {
            return GetImageList(_applicationPaths.GeneralPath, false);
        }

        /// <summary>
        ///     Get General Image.
        /// </summary>
        /// <param name="name">The name of the image.</param>
        /// <param name="type">Image Type (primary, backdrop, logo, etc).</param>
        /// <response code="200">Image stream retrieved.</response>
        /// <response code="404">Image not found.</response>
        /// <returns>A <see cref="FileStreamResult"/> containing the image contents on success, or a <see cref="NotFoundResult"/> if the image could not be found.</returns>
        [HttpGet("General/{name}/{type}")]
        [AllowAnonymous]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesImageFile]
        public ActionResult GetGeneralImage([FromRoute, Required] string name, [FromRoute, Required] string type)
        {
            var filename = string.Equals(type, "primary", StringComparison.OrdinalIgnoreCase)
                ? "folder"
                : type;

            var path = BaseItem.SupportedImageExtensions
                .Select(i => Path.GetFullPath(Path.Combine(_applicationPaths.GeneralPath, name, filename + i)))
                .FirstOrDefault(System.IO.File.Exists);

            if (path == null)
            {
                return NotFound();
            }

            if (!path.StartsWith(_applicationPaths.GeneralPath, StringComparison.InvariantCulture))
            {
                return BadRequest("Invalid image path.");
            }

            var contentType = MimeTypes.GetMimeType(path);
            return File(AsyncFile.OpenRead(path), contentType);
        }

        /// <summary>
        ///     Get all general images.
        /// </summary>
        /// <response code="200">Retrieved list of images.</response>
        /// <returns>An <see cref="OkResult"/> containing the list of images.</returns>
        [HttpGet("Ratings")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<ImageByNameInfo>> GetRatingImages()
        {
            return GetImageList(_applicationPaths.RatingsPath, false);
        }

        /// <summary>
        ///     Get rating image.
        /// </summary>
        /// <param name="theme">The theme to get the image from.</param>
        /// <param name="name">The name of the image.</param>
        /// <response code="200">Image stream retrieved.</response>
        /// <response code="404">Image not found.</response>
        /// <returns>A <see cref="FileStreamResult"/> containing the image contents on success, or a <see cref="NotFoundResult"/> if the image could not be found.</returns>
        [HttpGet("Ratings/{theme}/{name}")]
        [AllowAnonymous]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesImageFile]
        public ActionResult GetRatingImage(
            [FromRoute, Required] string theme,
            [FromRoute, Required] string name)
        {
            return GetImageFile(_applicationPaths.RatingsPath, theme, name);
        }

        /// <summary>
        ///     Get all media info images.
        /// </summary>
        /// <response code="200">Image list retrieved.</response>
        /// <returns>An <see cref="OkResult"/> containing the list of images.</returns>
        [HttpGet("MediaInfo")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<ImageByNameInfo>> GetMediaInfoImages()
        {
            return GetImageList(_applicationPaths.MediaInfoImagesPath, false);
        }

        /// <summary>
        ///     Get media info image.
        /// </summary>
        /// <param name="theme">The theme to get the image from.</param>
        /// <param name="name">The name of the image.</param>
        /// <response code="200">Image stream retrieved.</response>
        /// <response code="404">Image not found.</response>
        /// <returns>A <see cref="FileStreamResult"/> containing the image contents on success, or a <see cref="NotFoundResult"/> if the image could not be found.</returns>
        [HttpGet("MediaInfo/{theme}/{name}")]
        [AllowAnonymous]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesImageFile]
        public ActionResult GetMediaInfoImage(
            [FromRoute, Required] string theme,
            [FromRoute, Required] string name)
        {
            return GetImageFile(_applicationPaths.MediaInfoImagesPath, theme, name);
        }

        /// <summary>
        ///     Internal FileHelper.
        /// </summary>
        /// <param name="basePath">Path to begin search.</param>
        /// <param name="theme">Theme to search.</param>
        /// <param name="name">File name to search for.</param>
        /// <returns>A <see cref="FileStreamResult"/> containing the image contents on success, or a <see cref="NotFoundResult"/> if the image could not be found.</returns>
        private ActionResult GetImageFile(string basePath, string theme, string? name)
        {
            var themeFolder = Path.GetFullPath(Path.Combine(basePath, theme));

            if (Directory.Exists(themeFolder))
            {
                var path = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(themeFolder, name + i))
                    .FirstOrDefault(System.IO.File.Exists);

                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    if (!path.StartsWith(basePath, StringComparison.InvariantCulture))
                    {
                        return BadRequest("Invalid image path.");
                    }

                    var contentType = MimeTypes.GetMimeType(path);

                    return PhysicalFile(path, contentType);
                }
            }

            var allFolder = Path.GetFullPath(Path.Combine(basePath, "all"));
            if (Directory.Exists(allFolder))
            {
                var path = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(allFolder, name + i))
                    .FirstOrDefault(System.IO.File.Exists);

                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    if (!path.StartsWith(basePath, StringComparison.InvariantCulture))
                    {
                        return BadRequest("Invalid image path.");
                    }

                    var contentType = MimeTypes.GetMimeType(path);
                    return PhysicalFile(path, contentType);
                }
            }

            return NotFound();
        }

        private List<ImageByNameInfo> GetImageList(string path, bool supportsThemes)
        {
            try
            {
                return _fileSystem.GetFiles(path, BaseItem.SupportedImageExtensions, false, true)
                    .Select(i => new ImageByNameInfo
                    {
                        Name = _fileSystem.GetFileNameWithoutExtension(i),
                        FileLength = i.Length,

                        // For themeable images, use the Theme property
                        // For general images, the same object structure is fine,
                        // but it's not owned by a theme, so call it Context
                        Theme = supportsThemes ? GetThemeName(i.FullName, path) : null,
                        Context = supportsThemes ? null : GetThemeName(i.FullName, path),
                        Format = i.Extension.ToLowerInvariant().TrimStart('.')
                    })
                    .OrderBy(i => i.Name)
                    .ToList();
            }
            catch (IOException)
            {
                return new List<ImageByNameInfo>();
            }
        }

        private string? GetThemeName(string path, string rootImagePath)
        {
            var parentName = Path.GetDirectoryName(path);

            if (string.Equals(parentName, rootImagePath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            parentName = Path.GetFileName(parentName);

            return string.Equals(parentName, "all", StringComparison.OrdinalIgnoreCase) ? null : parentName;
        }
    }
}
