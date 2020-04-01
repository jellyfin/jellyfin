using System;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Providers
{
    public class MetadataRefreshOptions : ImageRefreshOptions
    {
        /// <summary>
        /// When paired with MetadataRefreshMode=FullRefresh, all existing data will be overwritten with new data from the providers.
        /// </summary>
        public bool ReplaceAllMetadata { get; set; }

        public MetadataRefreshMode MetadataRefreshMode { get; set; }

        public RemoteSearchResult SearchResult { get; set; }

        public string[] RefreshPaths { get; set; }

        public bool ForceSave { get; set; }

        public bool EnableRemoteContentProbe { get; set; }

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
            EnableRemoteContentProbe = copy.EnableRemoteContentProbe;

            ImageRefreshMode = copy.ImageRefreshMode;
            ReplaceAllImages = copy.ReplaceAllImages;
            ReplaceImages = copy.ReplaceImages;
            SearchResult = copy.SearchResult;

            if (copy.RefreshPaths != null && copy.RefreshPaths.Length > 0)
            {
                if (RefreshPaths == null)
                {
                    RefreshPaths = Array.Empty<string>();
                }

                RefreshPaths = copy.RefreshPaths.ToArray();
            }
        }

        public bool RefreshItem(BaseItem item)
        {
            if (RefreshPaths != null && RefreshPaths.Length > 0)
            {
                return RefreshPaths.Contains(item.Path ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            }

            return true;
        }
    }
}
