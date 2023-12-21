#nullable disable

using System;
using Emby.Naming.Common;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Class PhotoAlbumResolver.
    /// </summary>
    public class PhotoAlbumResolver : GenericFolderResolver<PhotoAlbum>
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly NamingOptions _namingOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoAlbumResolver"/> class.
        /// </summary>
        /// <param name="imageProcessor">The image processor.</param>
        /// <param name="namingOptions">The naming options.</param>
        public PhotoAlbumResolver(IImageProcessor imageProcessor, NamingOptions namingOptions)
        {
            _imageProcessor = imageProcessor;
            _namingOptions = namingOptions;
        }

        /// <inheritdoc />
        public override ResolverPriority Priority => ResolverPriority.Second;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override PhotoAlbum Resolve(ItemResolveArgs args)
        {
            // Must be an image file within a photo collection
            if (args.IsDirectory)
            {
                // Must be an image file within a photo collection
                var collectionType = args.GetCollectionType();

                if (collectionType == CollectionType.photos
                    || (collectionType == CollectionType.homevideos && args.LibraryOptions.EnablePhotos))
                {
                    if (HasPhotos(args))
                    {
                        return new PhotoAlbum
                        {
                            Path = args.Path
                        };
                    }
                }
            }

            return null;
        }

        private bool HasPhotos(ItemResolveArgs args)
        {
            var files = args.FileSystemChildren;

            foreach (var file in files)
            {
                if (!file.IsDirectory && PhotoResolver.IsImageFile(file.FullName, _imageProcessor))
                {
                    var filename = file.Name;
                    var ownedByMedia = false;

                    foreach (var siblingFile in files)
                    {
                        if (PhotoResolver.IsOwnedByMedia(_namingOptions, siblingFile.FullName, filename))
                        {
                            ownedByMedia = true;
                            break;
                        }
                    }

                    if (!ownedByMedia)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
