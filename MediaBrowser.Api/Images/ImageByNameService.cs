using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class GetGeneralImage
    /// </summary>
    [Route("/Images/General/{Name}/{Type}", "GET", Summary = "Gets a general image by name")]
    public class GetGeneralImage
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The name of the image", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        [ApiMember(Name = "Type", Description = "Image Type (primary, backdrop, logo, etc).", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Type { get; set; }
    }

    /// <summary>
    /// Class GetRatingImage
    /// </summary>
    [Route("/Images/Ratings/{Theme}/{Name}", "GET", Summary = "Gets a rating image by name")]
    public class GetRatingImage
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The name of the image", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the theme.
        /// </summary>
        /// <value>The theme.</value>
        [ApiMember(Name = "Theme", Description = "The theme to get the image from", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Theme { get; set; }
    }

    /// <summary>
    /// Class GetMediaInfoImage
    /// </summary>
    [Route("/Images/MediaInfo/{Theme}/{Name}", "GET", Summary = "Gets a media info image by name")]
    public class GetMediaInfoImage
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The name of the image", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the theme.
        /// </summary>
        /// <value>The theme.</value>
        [ApiMember(Name = "Theme", Description = "The theme to get the image from", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Theme { get; set; }
    }

    [Route("/Images/MediaInfo", "GET", Summary = "Gets all media info image by name")]
    [Authenticated]
    public class GetMediaInfoImages : IReturn<List<ImageByNameInfo>>
    {
    }

    [Route("/Images/Ratings", "GET", Summary = "Gets all rating images by name")]
    [Authenticated]
    public class GetRatingImages : IReturn<List<ImageByNameInfo>>
    {
    }

    [Route("/Images/General", "GET", Summary = "Gets all general images by name")]
    [Authenticated]
    public class GetGeneralImages : IReturn<List<ImageByNameInfo>>
    {
    }

    /// <summary>
    /// Class ImageByNameService
    /// </summary>
    public class ImageByNameService : BaseApiService
    {
        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IServerApplicationPaths _appPaths;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageByNameService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        public ImageByNameService(IServerApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public object Get(GetMediaInfoImages request)
        {
            return ToOptimizedResult(GetImageList(_appPaths.MediaInfoImagesPath, true));
        }

        public object Get(GetRatingImages request)
        {
            return ToOptimizedResult(GetImageList(_appPaths.RatingsPath, true));
        }

        public object Get(GetGeneralImages request)
        {
            return ToOptimizedResult(GetImageList(_appPaths.GeneralPath, false));
        }

        private List<ImageByNameInfo> GetImageList(string path, bool supportsThemes)
        {
            try
            {
				return _fileSystem.GetFiles(path, true)
                    .Where(i => BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.Ordinal))
                    .Select(i => new ImageByNameInfo
                    {
                        Name = _fileSystem.GetFileNameWithoutExtension(i),
                        FileLength = i.Length,

                        // For themeable images, use the Theme property
                        // For general images, the same object structure is fine,
                        // but it's not owned by a theme, so call it Context
                        Theme = supportsThemes ? GetThemeName(i.FullName, path) : null,
                        Context = supportsThemes ? null : GetThemeName(i.FullName, path),

                        Format = i.Extension.ToLower().TrimStart('.')
                    })
                    .OrderBy(i => i.Name)
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<ImageByNameInfo>();
            }
        }

        private string GetThemeName(string path, string rootImagePath)
        {
            var parentName = Path.GetDirectoryName(path);

            if (string.Equals(parentName, rootImagePath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            parentName = Path.GetFileName(parentName);

            return string.Equals(parentName, "all", StringComparison.OrdinalIgnoreCase) ?
                null :
                parentName;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGeneralImage request)
        {
            var filename = string.Equals(request.Type, "primary", StringComparison.OrdinalIgnoreCase)
                               ? "folder"
                               : request.Type;

            var paths = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(_appPaths.GeneralPath, request.Name, filename + i)).ToList();

			var path = paths.FirstOrDefault(_fileSystem.FileExists) ?? paths.FirstOrDefault();

            return ToStaticFileResult(path);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetRatingImage request)
        {
            var themeFolder = Path.Combine(_appPaths.RatingsPath, request.Theme);

			if (_fileSystem.DirectoryExists(themeFolder))
            {
                var path = BaseItem.SupportedImageExtensions
                    .Select(i => Path.Combine(themeFolder, request.Name + i))
					.FirstOrDefault(_fileSystem.FileExists);

                if (!string.IsNullOrEmpty(path))
                {
                    return ToStaticFileResult(path);
                }
            }

            var allFolder = Path.Combine(_appPaths.RatingsPath, "all");

			if (_fileSystem.DirectoryExists(allFolder))
            {
                // Avoid implicitly captured closure
                var currentRequest = request;

                var path = BaseItem.SupportedImageExtensions
                    .Select(i => Path.Combine(allFolder, currentRequest.Name + i))
					.FirstOrDefault(_fileSystem.FileExists);

                if (!string.IsNullOrEmpty(path))
                {
                    return ToStaticFileResult(path);
                }
            }

            throw new ResourceNotFoundException("MediaInfo image not found: " + request.Name);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetMediaInfoImage request)
        {
            var themeFolder = Path.Combine(_appPaths.MediaInfoImagesPath, request.Theme);

			if (_fileSystem.DirectoryExists(themeFolder))
            {
                var path = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(themeFolder, request.Name + i))
					.FirstOrDefault(_fileSystem.FileExists);

                if (!string.IsNullOrEmpty(path))
                {
                    return ToStaticFileResult(path);
                }
            }

            var allFolder = Path.Combine(_appPaths.MediaInfoImagesPath, "all");

			if (_fileSystem.DirectoryExists(allFolder))
            {
                // Avoid implicitly captured closure
                var currentRequest = request;

                var path = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(allFolder, currentRequest.Name + i))
					.FirstOrDefault(_fileSystem.FileExists);

                if (!string.IsNullOrEmpty(path))
                {
                    return ToStaticFileResult(path);
                }
            }

            throw new ResourceNotFoundException("MediaInfo image not found: " + request.Name);
        }
    }
}
