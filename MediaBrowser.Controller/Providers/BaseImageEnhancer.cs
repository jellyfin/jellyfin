using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Class BaseImageEnhancer
    /// </summary>
    public abstract class BaseImageEnhancer : IDisposable
    {
        /// <summary>
        /// Return true only if the given image for the given item will be enhanced by this enhancer.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns><c>true</c> if this enhancer will enhance the supplied image for the supplied item, <c>false</c> otherwise</returns>
        public abstract bool Supports(BaseItem item, ImageType imageType);

        /// <summary>
        /// Gets the priority or order in which this enhancer should be run.
        /// </summary>
        /// <value>The priority.</value>
        public abstract MetadataProviderPriority Priority { get; }

        /// <summary>
        /// Return the date of the last configuration change affecting the provided baseitem and image type
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns>Date of last config change</returns>
        public virtual DateTime LastConfigurationChange(BaseItem item, ImageType imageType)
        {
            return DateTime.MinValue;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
        }

        /// <summary>
        /// Gets the size of the enhanced image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="originalImageSize">Size of the original image.</param>
        /// <returns>ImageSize.</returns>
        public virtual ImageSize GetEnhancedImageSize(BaseItem item, ImageType imageType, int imageIndex, ImageSize originalImageSize)
        {
            return originalImageSize;
        }

        /// <summary>
        /// Enhances the supplied image and returns it
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="originalImage">The original image.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{System.Drawing.Image}.</returns>
        protected abstract Task<Image> EnhanceImageAsyncInternal(BaseItem item, Image originalImage, ImageType imageType, int imageIndex);

        /// <summary>
        /// Enhances the image async.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="originalImage">The original image.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{Image}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Task<Image> EnhanceImageAsync(BaseItem item, Image originalImage, ImageType imageType, int imageIndex)
        {
            if (item == null || originalImage == null)
            {
                throw new ArgumentNullException();
            }

            return EnhanceImageAsyncInternal(item, originalImage, imageType, imageIndex);
        }
    }
}
