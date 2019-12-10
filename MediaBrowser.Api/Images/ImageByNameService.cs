using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

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
        public ImageByNameService(
            ILogger<ImageByNameService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory resultFactory,
            IFileSystem fileSystem)
            : base(logger, serverConfigurationManager, resultFactory)
        {
            _appPaths = serverConfigurationManager.ApplicationPaths;
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
        public Task<object> Get(GetGeneralImage request)
        {
            var filename = string.Equals(request.Type, "primary", StringComparison.OrdinalIgnoreCase)
                               ? "folder"
                               : request.Type;

            var paths = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(_appPaths.GeneralPath, request.Name, filename + i)).ToList();

            var path = paths.FirstOrDefault(File.Exists) ?? paths.FirstOrDefault();

            return ResultFactory.GetStaticFileResult(Request, path);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetRatingImage request)
        {
            var themeFolder = Path.Combine(_appPaths.RatingsPath, request.Theme);

            if (Directory.Exists(themeFolder))
            {
                var path = BaseItem.SupportedImageExtensions
                    .Select(i => Path.Combine(themeFolder, request.Name + i))
                    .FirstOrDefault(File.Exists);

                if (!string.IsNullOrEmpty(path))
                {
                    return ResultFactory.GetStaticFileResult(Request, path);
                }
            }

            var allFolder = Path.Combine(_appPaths.RatingsPath, "all");

            if (Directory.Exists(allFolder))
            {
                // Avoid implicitly captured closure
                var currentRequest = request;

                var path = BaseItem.SupportedImageExtensions
                    .Select(i => Path.Combine(allFolder, currentRequest.Name + i))
                    .FirstOrDefault(File.Exists);

                if (!string.IsNullOrEmpty(path))
                {
                    return ResultFactory.GetStaticFileResult(Request, path);
                }
            }

            throw new ResourceNotFoundException("MediaInfo image not found: " + request.Name);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public Task<object> Get(GetMediaInfoImage request)
        {
            var themeFolder = Path.Combine(_appPaths.MediaInfoImagesPath, request.Theme);

            if (Directory.Exists(themeFolder))
            {
                var path = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(themeFolder, request.Name + i))
                    .FirstOrDefault(File.Exists);

                if (!string.IsNullOrEmpty(path))
                {
                    return ResultFactory.GetStaticFileResult(Request, path);
                }
            }

            var allFolder = Path.Combine(_appPaths.MediaInfoImagesPath, "all");

            if (Directory.Exists(allFolder))
            {
                // Avoid implicitly captured closure
                var currentRequest = request;

                var path = BaseItem.SupportedImageExtensions.Select(i => Path.Combine(allFolder, currentRequest.Name + i))
                    .FirstOrDefault(File.Exists);

                if (!string.IsNullOrEmpty(path))
                {
                    return ResultFactory.GetStaticFileResult(Request, path);
                }
            }

            throw new ResourceNotFoundException("MediaInfo image not found: " + request.Name);
        }
    }
}
