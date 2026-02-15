#pragma warning disable CS1591

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
    public class MusicAlbumImageProvider : BaseFolderImageProvider<MusicAlbum>
    {
        public MusicAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
        {
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var items = base.GetItemsWithImages(item);

            // Ignore any folders because they can have generated collages
            return items.Where(i => i is not Folder).ToList();
        }
    }
}
