using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using System.Drawing;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IImageEnhancer
    {
        /// <summary>
        /// Return true only if the given image for the given item will be enhanced by this enhancer.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns><c>true</c> if this enhancer will enhance the supplied image for the supplied item, <c>false</c> otherwise</returns>
        bool Supports(BaseItem item, ImageType imageType);

        /// <summary>
        /// Gets the priority or order in which this enhancer should be run.
        /// </summary>
        /// <value>The priority.</value>
        MetadataProviderPriority Priority { get; }

        /// <summary>
        /// Return a key incorporating all configuration information related to this item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns>Cache key relating to the current state of this item and configuration</returns>
        string GetConfigurationCacheKey(BaseItem item, ImageType imageType);

        /// <summary>
        /// Gets the size of the enhanced image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="originalImageSize">Size of the original image.</param>
        /// <returns>ImageSize.</returns>
        ImageSize GetEnhancedImageSize(BaseItem item, ImageType imageType, int imageIndex, ImageSize originalImageSize);

        /// <summary>
        /// Enhances the image async.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="originalImage">The original image.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{Image}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        Task<Image> EnhanceImageAsync(BaseItem item, Image originalImage, ImageType imageType, int imageIndex);
    }
}