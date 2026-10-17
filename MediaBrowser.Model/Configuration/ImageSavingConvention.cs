namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// The convention used for naming saved images.
    /// </summary>
    public enum ImageSavingConvention
    {
        /// <summary>
        /// The legacy naming convention.
        /// </summary>
        Legacy,

        /// <summary>
        /// The naming convention compatible with other media servers and metadata managers.
        /// </summary>
        Compatible
    }
}
