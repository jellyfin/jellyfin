using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Photos
{
    //public class PhotoAlbumImageProvider : BaseDynamicImageProvider<PhotoAlbum>
    //{
    //    public PhotoAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor)
    //        : base(fileSystem, providerManager, applicationPaths, imageProcessor)
    //    {
    //    }

    //    protected override Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
    //    {
    //        var photoAlbum = (PhotoAlbum)item;
    //        var items = GetFinalItems(photoAlbum.Children.ToList());

    //        return Task.FromResult(items);
    //    }
    //}
}
