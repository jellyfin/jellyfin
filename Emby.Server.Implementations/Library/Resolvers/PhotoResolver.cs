#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library.Resolvers
{
    public class PhotoResolver : ItemResolver<Photo>
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly ILibraryManager _libraryManager;
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

        public PhotoResolver(IImageProcessor imageProcessor, ILibraryManager libraryManager)
        {
            _imageProcessor = imageProcessor;
            _libraryManager = libraryManager;
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
                    || (string.Equals(collectionType, CollectionType.HomeVideos, StringComparison.OrdinalIgnoreCase) && args.GetLibraryOptions().EnablePhotos))
                {
                    if (IsImageFile(args.Path, _imageProcessor))
                    {
                        var filename = Path.GetFileNameWithoutExtension(args.Path);

                        // Make sure the image doesn't belong to a video file
                        var files = args.DirectoryService.GetFiles(Path.GetDirectoryName(args.Path));

                        foreach (var file in files)
                        {
                            if (IsOwnedByMedia(_libraryManager, file.FullName, filename))
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

        internal static bool IsOwnedByMedia(ILibraryManager libraryManager, string file, string imageFilename)
        {
            if (libraryManager.IsVideoFile(file))
            {
                return IsOwnedByResolvedMedia(libraryManager, file, imageFilename);
            }

            return false;
        }

        internal static bool IsOwnedByResolvedMedia(ILibraryManager libraryManager, string file, string imageFilename)
            => imageFilename.StartsWith(Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase);

        internal static bool IsImageFile(string path, IImageProcessor imageProcessor)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

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
            return imageProcessor.SupportedInputFormats.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}
