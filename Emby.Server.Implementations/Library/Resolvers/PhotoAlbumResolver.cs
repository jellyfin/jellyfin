using System;
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
    public class PhotoAlbumResolver : FolderResolver<PhotoAlbum>
    {
        private readonly IImageProcessor _imageProcessor;
        private ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoAlbumResolver"/> class.
        /// </summary>
        /// <param name="imageProcessor">The image processor.</param>
        /// <param name="libraryManager">The library manager.</param>
        public PhotoAlbumResolver(IImageProcessor imageProcessor, ILibraryManager libraryManager)
        {
            _imageProcessor = imageProcessor;
            _libraryManager = libraryManager;
        }

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

                if (string.Equals(collectionType, CollectionType.Photos, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(collectionType, CollectionType.HomeVideos, StringComparison.OrdinalIgnoreCase) && args.GetLibraryOptions().EnablePhotos))
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
                        if (PhotoResolver.IsOwnedByMedia(_libraryManager, siblingFile.FullName, filename))
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

        /// <inheritdoc />
        public override ResolverPriority Priority => ResolverPriority.Second;
    }
}
