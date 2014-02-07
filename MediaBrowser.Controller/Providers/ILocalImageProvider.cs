using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

    public interface IDynamicImageProvider : IImageProvider
    {
        /// <summary>
        /// Gets the supported images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{ImageType}.</returns>
        IEnumerable<ImageType> GetSupportedImages(IHasImages item);

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{DynamicImageResponse}.</returns>
        Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken);
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
        public bool HasImage { get; set; }

        public void SetFormatFromMimeType(string mimeType)
        {

        }
    }
}
