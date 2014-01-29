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

        /// <summary>
        /// TODO: deprecate. Keeping this for now, for api compatibility
        /// </summary>
        [Obsolete]
        public bool ResetResolveArgs { get; set; }
    }

    public class ImageRefreshOptions
    {
        public MetadataRefreshMode ImageRefreshMode { get; set; }
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
}
