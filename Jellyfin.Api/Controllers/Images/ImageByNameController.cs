#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers.Images
{
    /// <summary>
    ///     Images By Name Controller.
    /// </summary>
    [Route("Images")]
    [Authenticated]
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
        /// <returns>General images.</returns>
        [HttpGet("General")]
        [ProducesResponseType(typeof(ImageByNameInfo[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetGeneralImages()
        {
            return Ok(GetImageList(_applicationPaths.GeneralPath, false));
        }

        /// <summary>
        ///     Get General Image.
        /// </summary>
        /// <param name="name">The name of the image.</param>
        /// <param name="type">Image Type (primary, backdrop, logo, etc).</param>
        /// <returns>Image Stream.</returns>
        [HttpGet("General/{Name}/{Type}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetGeneralImage([FromRoute] string name, [FromRoute] string type)
        {
            var filename = string.Equals(type, "primary", StringComparison.OrdinalIgnoreCase)
                ? "folder"
                : type;

            var paths = BaseItem.SupportedImageExtensions
                .Select(i => Path.Combine(_applicationPaths.GeneralPath, name, filename + i)).ToList();

            var path = paths.FirstOrDefault(System.IO.File.Exists) ?? paths.FirstOrDefault();
            if (path == null || !System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var contentType = MimeTypes.GetMimeType(path);
            return new FileStreamResult(System.IO.File.OpenRead(path), contentType);
        }

        /// <summary>
        ///     Get all general images.
        /// </summary>
        /// <returns>General images.</returns>
        [HttpGet("Ratings")]
        [ProducesResponseType(typeof(ImageByNameInfo[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetRatingImages()
        {
            return Ok(GetImageList(_applicationPaths.RatingsPath, false));
        }

        /// <summary>
        ///     Get rating image.
        /// </summary>
        /// <param name="theme">The theme to get the image from.</param>
        /// <param name="name">The name of the image.</param>
        /// <returns>Image Stream.</returns>
        [HttpGet("Ratings/{Theme}/{Name}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetRatingImage(
            [FromRoute] string theme,
            [FromRoute] string name)
        {
            return GetImageFile(_applicationPaths.RatingsPath, theme, name);
        }

        /// <summary>
        ///     Get all media info images.
        /// </summary>
        /// <returns>Media Info images.</returns>
        [HttpGet("MediaInfo")]
        [ProducesResponseType(typeof(ImageByNameInfo[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetMediaInfoImages()
        {
            return Ok(GetImageList(_applicationPaths.MediaInfoImagesPath, false));
        }

        /// <summary>
        ///     Get media info image.
        /// </summary>
        /// <param name="theme">The theme to get the image from.</param>
        /// <param name="name">The name of the image.</param>
        /// <returns>Image Stream.</returns>
        [HttpGet("MediaInfo/{Theme}/{Name}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetMediaInfoImage(
            [FromRoute] string theme,
            [FromRoute] string name)
        {
            return GetImageFile(_applicationPaths.MediaInfoImagesPath, theme, name);
        }

        /// <summary>
        ///     Internal FileHelper.
        /// </summary>
        /// <param name="basePath">Path to begin search.</param>
        /// <param name="theme">Theme to search.</param>
        /// <param name="name">File name to search for.</param>
        /// <returns>Image Stream.</returns>
        private IActionResult GetImageFile(string basePath, string theme, string name)
        {
            var themeFolder = Path.Combine(basePath, theme);
            if (Directory.Exists(themeFolder))
            {
                var path = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(themeFolder, name + i))
                    .FirstOrDefault(System.IO.File.Exists);

                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    var contentType = MimeTypes.GetMimeType(path);
                    return new FileStreamResult(System.IO.File.OpenRead(path), contentType);
                }
            }

            var allFolder = Path.Combine(basePath, "all");
            if (Directory.Exists(allFolder))
            {
                var path = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(allFolder, name + i))
                    .FirstOrDefault(System.IO.File.Exists);

                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    var contentType = MimeTypes.GetMimeType(path);
                    return new FileStreamResult(System.IO.File.OpenRead(path), contentType);
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
