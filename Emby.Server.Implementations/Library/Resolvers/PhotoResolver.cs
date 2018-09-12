using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library.Resolvers
{
    public class PhotoResolver : ItemResolver<Photo>
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        public PhotoResolver(IImageProcessor imageProcessor, ILibraryManager libraryManager, IFileSystem fileSystem)
        {
            _imageProcessor = imageProcessor;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
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
                        var files = args.DirectoryService.GetFiles(_fileSystem.GetDirectoryName(args.Path));
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

        private static readonly string[] IgnoreFiles =
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

            if (IgnoreFiles.Contains(filename, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (IgnoreFiles.Any(i => filename.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return false;
            }

            return imageProcessor.SupportedInputFormats.Contains((Path.GetExtension(path) ?? string.Empty).TrimStart('.'), StringComparer.OrdinalIgnoreCase);
        }

    }
}
