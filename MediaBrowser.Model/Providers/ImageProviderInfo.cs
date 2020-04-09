using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Class ImageProviderInfo.
    /// </summary>
    public class ImageProviderInfo
    {
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
