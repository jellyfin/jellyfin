using System.Linq;
using CommonIO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Providers
{
    public class MetadataRefreshOptions : ImageRefreshOptions
    {
        /// <summary>
        /// When paired with MetadataRefreshMode=FullRefresh, all existing data will be overwritten with new data from the providers.
        /// </summary>
        public bool ReplaceAllMetadata { get; set; }

        public bool IsPostRecursiveRefresh { get; set; }

        public MetadataRefreshMode MetadataRefreshMode { get; set; }
        public RemoteSearchResult SearchResult { get; set; }

        public bool ForceSave { get; set; }

        public MetadataRefreshOptions(IFileSystem fileSystem)
			: this(new DirectoryService(new NullLogger(), fileSystem))
        {
        }

        public MetadataRefreshOptions(IDirectoryService directoryService)
            : base(directoryService)
        {
            MetadataRefreshMode = MetadataRefreshMode.Default;
        }

        public MetadataRefreshOptions(MetadataRefreshOptions copy)
            : base(copy.DirectoryService)
        {
            MetadataRefreshMode = copy.MetadataRefreshMode;
            ForceSave = copy.ForceSave;
            ReplaceAllMetadata = copy.ReplaceAllMetadata;

            ImageRefreshMode = copy.ImageRefreshMode;
            ReplaceAllImages = copy.ReplaceAllImages;
            ReplaceImages = copy.ReplaceImages.ToList();
            SearchResult = copy.SearchResult;
        }
    }
}
