#pragma warning disable CS1591

using System;
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
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public ImageType[] SupportedImages { get; set; }
    }
}
