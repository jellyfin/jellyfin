using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;

namespace Emby.Server.Implementations.Library.Resolvers
{
    public class PhotoAlbumResolver : FolderResolver<PhotoAlbum>
    {
        private readonly IImageProcessor _imageProcessor;
        public PhotoAlbumResolver(IImageProcessor imageProcessor)
        {
            _imageProcessor = imageProcessor;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override PhotoAlbum Resolve(ItemResolveArgs args)
        {
            // Must be an image file within a photo collection
            if (args.IsDirectory && string.Equals(args.GetCollectionType(), CollectionType.Photos, StringComparison.OrdinalIgnoreCase))
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

        private bool HasPhotos(ItemResolveArgs args)
        {
            return args.FileSystemChildren.Any(i => (!i.IsDirectory) && PhotoResolver.IsImageFile(i.FullName, _imageProcessor));
        }

        public override ResolverPriority Priority
        {
            get
            {
                // Behind special folder resolver
                return ResolverPriority.Second;
            }
        }
    }
}
