using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Photos
{
    public class PhotoAlbumImageProvider : BaseDynamicImageProvider<PhotoAlbum>, ICustomMetadataProvider<PhotoAlbum>
    {
        public PhotoAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager)
            : base(fileSystem, providerManager)
        {
        }

        protected override Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var photoAlbum = (PhotoAlbum)item;
            var items = GetFinalItems(photoAlbum.RecursiveChildren.Where(i => i is Photo).ToList());

            return Task.FromResult(items);
        }
    }
}
