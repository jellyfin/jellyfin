using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Class ImageProviderInfo.
    /// </summary>
    public class ImageProviderInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageProviderInfo" /> class.
        /// </summary>
        /// <param name="name">The name of the image provider.</param>
        /// <param name="supportedImages">The image types supported by the image provider.</param>
        public ImageProviderInfo(string name, ImageType[] supportedImages)
        {
            Name = name;
            SupportedImages = supportedImages;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the supported image types.
        /// </summary>
        public ImageType[] SupportedImages { get; }
    }
}
