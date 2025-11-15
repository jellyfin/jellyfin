using System.Collections.Generic;
using System.Linq;
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
    /// Provides images for music albums.
    /// </summary>
    public class MusicAlbumImageProvider : BaseFolderImageProvider<MusicAlbum>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MusicAlbumImageProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="imageProcessor">The image processor.</param>
        /// <param name="libraryManager">The library manager.</param>
        public MusicAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var items = base.GetItemsWithImages(item);

            // Ignore any folders because they can have generated collages
            return items.Where(i => i is not Folder).ToList();
        }
    }
}
