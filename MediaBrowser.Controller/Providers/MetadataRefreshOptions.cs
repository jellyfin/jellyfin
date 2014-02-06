using System;

namespace MediaBrowser.Controller.Providers
{
    public class MetadataRefreshOptions : ImageRefreshOptions
    {
        /// <summary>
        /// When paired with MetadataRefreshMode=FullRefresh, all existing data will be overwritten with new data from the providers.
        /// </summary>
        public bool ReplaceAllMetadata { get; set; }

        public MetadataRefreshMode MetadataRefreshMode { get; set; }

        /// <summary>
        /// TODO: deprecate. Keeping this for now, for api compatibility
        /// </summary>
        [Obsolete]
        public bool ForceSave { get; set; }
    }

    public class ImageRefreshOptions
    {
        public ImageRefreshMode ImageRefreshMode { get; set; }

        public ImageRefreshOptions()
        {
            ImageRefreshMode = ImageRefreshMode.Default;
        }
    }

    public enum MetadataRefreshMode
    {
        /// <summary>
        /// Providers will be executed based on default rules
        /// </summary>
        EnsureMetadata,

        /// <summary>
        /// No providers will be executed
        /// </summary>
        None,

        /// <summary>
        /// All providers will be executed to search for new metadata
        /// </summary>
        FullRefresh
    }

    public enum ImageRefreshMode
    {
        /// <summary>
        /// The default
        /// </summary>
        Default,

        /// <summary>
        /// Existing images will be validated
        /// </summary>
        ValidationOnly,

        /// <summary>
        /// All providers will be executed to search for new metadata
        /// </summary>
        FullRefresh
    }
}
