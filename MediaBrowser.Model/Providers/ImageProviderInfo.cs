#pragma warning disable CS1591
#pragma warning disable CA1819 // Properties should not return arrays

using System;

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Class ImageProviderInfo.
    /// </summary>
    public class ImageProviderInfo
    {
        public ImageProviderInfo()
        {
            SupportedImages = Array.Empty<ImageType>();
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public ImageType[] SupportedImages { get; set; }
    }
}
