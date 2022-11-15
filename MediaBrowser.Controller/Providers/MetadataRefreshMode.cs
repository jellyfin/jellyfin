#pragma warning disable CS1591

namespace MediaBrowser.Controller.Providers
{
    public enum MetadataRefreshMode
    {
        /// <summary>
        /// The none.
        /// </summary>
        None = 0,

        /// <summary>
        /// The validation only.
        /// </summary>
        ValidationOnly = 1,

        /// <summary>
        /// Providers will be executed based on default rules.
        /// </summary>
        Default = 2,

        /// <summary>
        /// All providers will be executed to search for new metadata.
        /// </summary>
        FullRefresh = 3
    }
}
