using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// This is just a marker interface
    /// </summary>
    public interface ILocalImageProvider : IImageProvider
    {
    }

    public interface IImageFileProvider : ILocalImageProvider
    {
        List<LocalImageInfo> GetImages(IHasImages item);
    }

    public class LocalImageInfo
    {
        public string Path { get; set; }
        public ImageType Type { get; set; }
    }

    public interface IDynamicImageProvider : ILocalImageProvider
    {
        /// <summary>
        /// Gets the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List{DynamicImageInfo}.</returns>
        List<DynamicImageInfo> GetImageInfos(IHasImages item);

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="info">The information.</param>
        /// <returns>Task{DynamicImageResponse}.</returns>
        Task<DynamicImageResponse> GetImage(IHasImages item, DynamicImageInfo info);
    }

    public class DynamicImageInfo
    {
        public string ImageId { get; set; }
        public ImageType Type { get; set; }
    }

    public class DynamicImageResponse
    {
        public string Path { get; set; }
        public Stream Stream { get; set; }
        public ImageFormat Format { get; set; }
    }
}
