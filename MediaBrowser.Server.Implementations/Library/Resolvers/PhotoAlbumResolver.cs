using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    public class PhotoAlbumResolver : FolderResolver<PhotoAlbum>
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override PhotoAlbum Resolve(ItemResolveArgs args)
        {
            // Must be an image file within a photo collection
            if (!args.IsRoot && args.IsDirectory && string.Equals(args.GetCollectionType(), CollectionType.Photos, StringComparison.OrdinalIgnoreCase))
            {
                if (HasPhotos(args))
                {
                    return new PhotoAlbum
                    {
                        Path = args.Path
                    };
                }
            }

            return null;
        }

        private static bool HasPhotos(ItemResolveArgs args)
        {
            return args.FileSystemChildren.Any(i => ((i.Attributes & FileAttributes.Directory) != FileAttributes.Directory) && PhotoResolver.IsImageFile(i.FullName));
        }
    }
}
