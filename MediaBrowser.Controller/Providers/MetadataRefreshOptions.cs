#nullable disable

#pragma warning disable CA1819, CS1591

using System;
using System.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Providers
{
    public class MetadataRefreshOptions : ImageRefreshOptions
    {
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

            IsAutomated = copy.IsAutomated;
            ImageRefreshMode = copy.ImageRefreshMode;
            ReplaceAllImages = copy.ReplaceAllImages;
            RegenerateTrickplay = copy.RegenerateTrickplay;
            ReplaceImages = copy.ReplaceImages;
            SearchResult = copy.SearchResult;
            RemoveOldMetadata = copy.RemoveOldMetadata;

            if (copy.RefreshPaths is not null && copy.RefreshPaths.Length > 0)
            {
                RefreshPaths ??= Array.Empty<string>();

                RefreshPaths = copy.RefreshPaths.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether all existing data should be overwritten with new data from providers
        /// when paired with MetadataRefreshMode=FullRefresh.
        /// </summary>
        public bool ReplaceAllMetadata { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all existing trickplay images should be overwritten
        /// when paired with MetadataRefreshMode=FullRefresh.
        /// </summary>
        public bool RegenerateTrickplay { get; set; }

        public MetadataRefreshMode MetadataRefreshMode { get; set; }

        public RemoteSearchResult SearchResult { get; set; }

        public string[] RefreshPaths { get; set; }

        public bool ForceSave { get; set; }

        public bool EnableRemoteContentProbe { get; set; }

        public bool RefreshItem(BaseItem item)
        {
            if (RefreshPaths is not null && RefreshPaths.Length > 0)
            {
                return RefreshPaths.Contains(item.Path ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }
    }
}
