using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Class RemoteImageResult.
    /// </summary>
    public class RemoteImageResult
    {
        /// <summary>
        /// Gets or sets the images.
        /// </summary>
        /// <value>The images.</value>
        public List<RemoteImageInfo> Images { get; set; }

        /// <summary>
        /// Gets or sets the total record count.
        /// </summary>
        /// <value>The total record count.</value>
        public int TotalRecordCount { get; set; }

        /// <summary>
        /// Gets or sets the providers.
        /// </summary>
        /// <value>The providers.</value>
        public List<string> Providers { get; set; }
    }

    public class RemoteImageQuery
    {
        public string ProviderName { get; set; }

        public ImageType? ImageType { get; set; }

        public bool IncludeDisabledProviders { get; set; }

        public bool IncludeAllLanguages { get; set; }
    }
}
