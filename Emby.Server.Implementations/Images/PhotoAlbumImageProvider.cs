#pragma warning disable CS1591

using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Images;

public class PhotoAlbumImageProvider : BaseFolderImageProvider<PhotoAlbum>
{
    public PhotoAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
        : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
    {
    }
}
