using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;

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
            var items = GetFinalItems(photoAlbum.Children.ToList(), 1);

            return Task.FromResult(items);
        }

        protected override async Task<string> CreateImage(IHasImages item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, Model.Entities.ImageType imageType, int imageIndex)
        {
            var photoFile = itemsWithImages.Where(i => Path.HasExtension(i.Path)).Select(i => i.Path).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(photoFile))
            {
                return null;
            }

            var ext = Path.GetExtension(photoFile);

            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ext);
            File.Copy(photoFile, outputPath);

            return outputPath;
        }
    }
}
