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
            if (!args.IsDirectory && IsImageFile(args.Path) && string.Equals(args.GetCollectionType(), CollectionType.Photos, StringComparison.OrdinalIgnoreCase))
            {
                return new Photo
                {
                    Path = args.Path
                };
            }

            return null;
        }

        protected static string[] ImageExtensions = { ".tiff", ".jpeg", ".jpg", ".png", ".aiff" };
        internal static bool IsImageFile(string path)
        {
            var filename = Path.GetFileName(path);

            return !string.Equals(filename, "folder.jpg", StringComparison.OrdinalIgnoreCase)
                && ImageExtensions.Contains(Path.GetExtension(path) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

    }
}
