using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

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

        public MetadataRefreshOptions()
        {
            MetadataRefreshMode = MetadataRefreshMode.Default;
        }
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
            return ImageRefreshMode == ImageRefreshMode.FullRefresh &&
                (ReplaceAllImages || ReplaceImages.Contains(type));
        }
    }

    public enum MetadataRefreshMode
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 0,

        /// <summary>
        /// The validation only
        /// </summary>
        ValidationOnly = 1,

        /// <summary>
        /// Providers will be executed based on default rules
        /// </summary>
        Default = 2,

        /// <summary>
        /// All providers will be executed to search for new metadata
        /// </summary>
        FullRefresh = 3
    }

    public enum ImageRefreshMode
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 0,

        /// <summary>
        /// The default
        /// </summary>
        Default = 1,

        /// <summary>
        /// Existing images will be validated
        /// </summary>
        ValidationOnly = 2,

        /// <summary>
        /// All providers will be executed to search for new metadata
        /// </summary>
        FullRefresh = 3
    }
}
