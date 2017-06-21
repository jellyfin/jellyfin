using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Images;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Photos
{
    public class PhotoAlbumImageProvider : BaseDynamicImageProvider<PhotoAlbum>
    {
        public PhotoAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        protected override List<BaseItem> GetItemsWithImages(IHasImages item)
        {
            var photoAlbum = (PhotoAlbum)item;
            var items = GetFinalItems(photoAlbum.Children.ToList());

            return items;
        }

        protected override string CreateImage(IHasImages item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            return CreateSingleImage(itemsWithImages, outputPathWithoutExtension, ImageType.Primary);
        }
    }
}
