using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Images
{
    /// <summary>
    /// Provides folder images.
    /// </summary>
    public class FolderImageProvider : BaseFolderImageProvider<Folder>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FolderImageProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="imageProcessor">The image processor.</param>
        /// <param name="libraryManager">The library manager.</param>
        public FolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override bool Supports(BaseItem item)
        {
            if (item is PhotoAlbum || item is MusicAlbum)
            {
                return false;
            }

            if (item is Folder && item.IsTopParent)
            {
                return false;
            }

            return true;
        }
    }
}
