#pragma warning disable CS1591

using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Images
{
    public class FolderImageProvider : BaseFolderImageProvider<Folder>
    {
        public FolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
        {
        }

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
