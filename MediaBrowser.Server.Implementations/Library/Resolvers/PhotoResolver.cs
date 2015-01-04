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
                IsImageFile(args.Path))
            {
                return new Photo
                {
                    Path = args.Path
                };
            }

            return null;
        }

        protected static string[] ImageExtensions = { ".tiff", ".jpeg", ".jpg", ".png", ".aiff" };

        private static readonly string[] IgnoreFiles =
        {
            "folder",
            "thumb",
            "landscape",
            "fanart",
            "backdrop",
            "poster"
        };

        internal static bool IsImageFile(string path)
        {
            var filename = Path.GetFileNameWithoutExtension(path) ?? string.Empty;

            return !IgnoreFiles.Contains(filename, StringComparer.OrdinalIgnoreCase)
                && ImageExtensions.Contains(Path.GetExtension(path) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

    }
}
