using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    public class PhotoResolver : ItemResolver<Photo>
    {
        private readonly IServerApplicationPaths _applicationPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoResolver" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        public PhotoResolver(IServerApplicationPaths applicationPaths)
        {
            _applicationPaths = applicationPaths;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override Photo Resolve(ItemResolveArgs args)
        {
            // Must be an image file within a photo collection
            if (!args.IsDirectory && IsImageFile(args.Path) && string.Equals(args.GetCollectionType(), "photos", StringComparison.OrdinalIgnoreCase))
            {
                return new Photo
                {
                    Path = args.Path
                };
            }

            return null;
        }

        protected static string[] ImageExtensions = { ".tiff", ".jpg", ".png", ".aiff" };
        protected bool IsImageFile(string path)
        {
            return !path.EndsWith("folder.jpg", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith("backdrop.jpg", StringComparison.OrdinalIgnoreCase)
                && ImageExtensions.Any(p => path.EndsWith(p, StringComparison.OrdinalIgnoreCase));
        }

    }
}
