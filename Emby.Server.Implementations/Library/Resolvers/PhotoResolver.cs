#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Class PhotoResolver.
    /// </summary>
    public class PhotoResolver : ItemResolver<Photo>
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly NamingOptions _namingOptions;
        private readonly IDirectoryService _directoryService;

        private static readonly HashSet<string> _ignoreFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "folder",
            "thumb",
            "landscape",
            "fanart",
            "backdrop",
            "poster",
            "cover",
            "logo",
            "default"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoResolver"/> class.
        /// </summary>
        /// <param name="imageProcessor">The image processor.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="directoryService">The directory service.</param>
        public PhotoResolver(IImageProcessor imageProcessor, NamingOptions namingOptions, IDirectoryService directoryService)
        {
            _imageProcessor = imageProcessor;
            _namingOptions = namingOptions;
            _directoryService = directoryService;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override Photo Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory)
            {
                // Must be an image file within a photo collection
                var collectionType = args.CollectionType;

                if (string.Equals(collectionType, CollectionType.Photos, StringComparison.OrdinalIgnoreCase)
                    || (string.Equals(collectionType, CollectionType.HomeVideos, StringComparison.OrdinalIgnoreCase) && args.LibraryOptions.EnablePhotos))
                {
                    if (IsImageFile(args.Path, _imageProcessor))
                    {
                        var filename = Path.GetFileNameWithoutExtension(args.Path);

                        // Make sure the image doesn't belong to a video file
                        var files = _directoryService.GetFiles(Path.GetDirectoryName(args.Path));

                        foreach (var file in files)
                        {
                            if (IsOwnedByMedia(_namingOptions, file.FullName, filename))
                            {
                                return null;
                            }
                        }

                        return new Photo
                        {
                            Path = args.Path
                        };
                    }
                }
            }

            return null;
        }

        internal static bool IsOwnedByMedia(NamingOptions namingOptions, string file, string imageFilename)
        {
            return VideoResolver.IsVideoFile(file, namingOptions) && IsOwnedByResolvedMedia(file, imageFilename);
        }

        internal static bool IsOwnedByResolvedMedia(string file, string imageFilename)
            => imageFilename.StartsWith(Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase);

        internal static bool IsImageFile(string path, IImageProcessor imageProcessor)
        {
            ArgumentNullException.ThrowIfNull(path);

            var filename = Path.GetFileNameWithoutExtension(path);

            if (_ignoreFiles.Contains(filename))
            {
                return false;
            }

            if (_ignoreFiles.Any(i => filename.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return false;
            }

            string extension = Path.GetExtension(path).TrimStart('.');
            return imageProcessor.SupportedInputFormats.Contains(extension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
