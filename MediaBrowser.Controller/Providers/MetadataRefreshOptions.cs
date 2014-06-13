using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

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
        public IDirectoryService DirectoryService { get; set; }

        public bool ReplaceAllImages { get; set; }

        public List<ImageType> ReplaceImages { get; set; }

        public ImageRefreshOptions()
        {
            ImageRefreshMode = ImageRefreshMode.Default;

            ReplaceImages = new List<ImageType>();
        }

        public bool IsReplacingImage(ImageType type)
        {
            return ReplaceAllImages || ReplaceImages.Contains(type);
        }
    }

    public enum MetadataRefreshMode
    {
        /// <summary>
        /// Providers will be executed based on default rules
        /// </summary>
        EnsureMetadata = 0,

        /// <summary>
        /// No providers will be executed
        /// </summary>
        None = 1,

        /// <summary>
        /// All providers will be executed to search for new metadata
        /// </summary>
        FullRefresh = 2,

        /// <summary>
        /// The validation only
        /// </summary>
        ValidationOnly = 3
    }

    public enum ImageRefreshMode
    {
        /// <summary>
        /// The default
        /// </summary>
        Default = 0,

        /// <summary>
        /// Existing images will be validated
        /// </summary>
        ValidationOnly = 1,

        /// <summary>
        /// All providers will be executed to search for new metadata
        /// </summary>
        FullRefresh = 2
    }
}
