using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    public class PhotoResolver : ItemResolver<Photo>
    {
        private readonly IImageProcessor _imageProcessor;
        public PhotoResolver(IImageProcessor imageProcessor)
        {
            _imageProcessor = imageProcessor;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override Photo Resolve(ItemResolveArgs args)
        {
            // Must be an image file within a photo collection
            if (string.Equals(args.GetCollectionType(), CollectionType.Photos, StringComparison.OrdinalIgnoreCase) &&
                !args.IsDirectory &&
                IsImageFile(args.Path, _imageProcessor))
            {
                return new Photo
                {
                    Path = args.Path
                };
            }

            return null;
        }

        private static readonly string[] IgnoreFiles =
        {
            "folder",
            "thumb",
            "landscape",
            "fanart",
            "backdrop",
            "poster"
        };

        internal static bool IsImageFile(string path, IImageProcessor imageProcessor)
        {
            var filename = Path.GetFileNameWithoutExtension(path) ?? string.Empty;

            if (IgnoreFiles.Contains(filename, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (IgnoreFiles.Any(i => filename.IndexOf("-" + i, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return false;
            }

            return imageProcessor.SupportedInputFormats.Contains((Path.GetExtension(path) ?? string.Empty).TrimStart('.'), StringComparer.OrdinalIgnoreCase);
        }

    }
}
