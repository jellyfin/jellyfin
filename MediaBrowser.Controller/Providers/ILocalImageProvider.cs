using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
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

    public interface ILocalImageFileProvider : ILocalImageProvider
    {
        List<LocalImageInfo> GetImages(IHasImages item, IDirectoryService directoryService);
    }

    public class LocalImageInfo
    {
        public FileInfo FileInfo { get; set; }
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
            if (mimeType.EndsWith("gif", StringComparison.OrdinalIgnoreCase))
            {
                Format = ImageFormat.Gif;
            }
            else if (mimeType.EndsWith("bmp", StringComparison.OrdinalIgnoreCase))
            {
                Format = ImageFormat.Bmp;
            }
            else if (mimeType.EndsWith("png", StringComparison.OrdinalIgnoreCase))
            {
                Format = ImageFormat.Png;
            }
            else
            {
                Format = ImageFormat.Jpg;
            }
        }
    }
}
