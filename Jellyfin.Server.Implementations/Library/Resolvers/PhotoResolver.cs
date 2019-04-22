using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Controller.Drawing;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Library;
using Jellyfin.Model.Configuration;
using Jellyfin.Model.Entities;

namespace Jellyfin.Server.Implementations.Library.Resolvers
{
    public class PhotoResolver : ItemResolver<Photo>
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly ILibraryManager _libraryManager;

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
                var collectionType = args.GetCollectionType();

                if (string.Equals(collectionType, CollectionType.Photos, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(collectionType, CollectionType.HomeVideos, StringComparison.OrdinalIgnoreCase) && args.GetLibraryOptions().EnablePhotos))
                {
                    if (IsImageFile(args.Path, _imageProcessor))
                    {
                        var filename = Path.GetFileNameWithoutExtension(args.Path);

                        // Make sure the image doesn't belong to a video file
                        var files = args.DirectoryService.GetFiles(Path.GetDirectoryName(args.Path));
                        var libraryOptions = args.GetLibraryOptions();

                        foreach (var file in files)
                        {
                            if (IsOwnedByMedia(_libraryManager, libraryOptions, file.FullName, filename))
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

        internal static bool IsOwnedByMedia(ILibraryManager libraryManager, LibraryOptions libraryOptions, string file, string imageFilename)
        {
            if (libraryManager.IsVideoFile(file, libraryOptions))
            {
                return IsOwnedByResolvedMedia(libraryManager, libraryOptions, file, imageFilename);
            }

            return false;
        }

        internal static bool IsOwnedByResolvedMedia(ILibraryManager libraryManager, LibraryOptions libraryOptions, string file, string imageFilename)
        {
            if (imageFilename.StartsWith(Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static readonly HashSet<string> IgnoreFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        internal static bool IsImageFile(string path, IImageProcessor imageProcessor)
        {
            var filename = Path.GetFileNameWithoutExtension(path) ?? string.Empty;

            if (IgnoreFiles.Contains(filename))
            {
                return false;
            }

            if (IgnoreFiles.Any(i => filename.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return false;
            }

            return imageProcessor.SupportedInputFormats.Contains(Path.GetExtension(path).TrimStart('.'), StringComparer.Ordinal);
        }
    }
}
