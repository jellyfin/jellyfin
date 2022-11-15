#nullable disable
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
        public RemoteImageInfo[] Images { get; set; }

        /// <summary>
        /// Gets or sets the total record count.
        /// </summary>
        /// <value>The total record count.</value>
        public int TotalRecordCount { get; set; }

        /// <summary>
        /// Gets or sets the providers.
        /// </summary>
        /// <value>The providers.</value>
        public string[] Providers { get; set; }
    }
}
