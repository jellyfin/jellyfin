using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Server.Implementations.Photos
{
    public class PhotoAlbumImageProvider : BaseDynamicImageProvider<PhotoAlbum>
    {
        public PhotoAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        protected override Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var photoAlbum = (PhotoAlbum)item;
            var items = GetFinalItems(photoAlbum.Children.ToList());

            return Task.FromResult(items);
        }

        protected override async Task<string> CreateImage(IHasImages item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            var image = itemsWithImages
                .Where(i => i.HasImage(ImageType.Primary) && i.GetImageInfo(ImageType.Primary, 0).IsLocalFile && Path.HasExtension(i.GetImagePath(ImageType.Primary)))
                .Select(i => i.GetImagePath(ImageType.Primary))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(image))
            {
                return null;
            }

            var ext = Path.GetExtension(image);

            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ext);
            File.Copy(image, outputPath);

            return outputPath;
        }
    }
}
