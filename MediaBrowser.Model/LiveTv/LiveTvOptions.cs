#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.LiveTv
{
    public class LiveTvOptions
    {
        public LiveTvOptions()
        {
            TunerHosts = Array.Empty<TunerHostInfo>();
            ListingProviders = Array.Empty<ListingsProviderInfo>();
            MediaLocationsCreated = Array.Empty<string>();
            RecordingPostProcessorArguments = "\"{path}\"";
        }

        public int? GuideDays { get; set; }

        public string RecordingPath { get; set; }

        public string MovieRecordingPath { get; set; }

        public string SeriesRecordingPath { get; set; }

        public bool EnableRecordingSubfolders { get; set; }

        public bool EnableOriginalAudioWithEncodedRecordings { get; set; }

        public TunerHostInfo[] TunerHosts { get; set; }

        public ListingsProviderInfo[] ListingProviders { get; set; }

        public int PrePaddingSeconds { get; set; }

        public int PostPaddingSeconds { get; set; }

        public string[] MediaLocationsCreated { get; set; }

        public string RecordingPostProcessor { get; set; }

        public string RecordingPostProcessorArguments { get; set; }

        public bool SaveRecordingNFO { get; set; } = true;

        public bool SaveRecordingImages { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether channels without any guide data are hidden
        /// from the channel list and guide. Useful for large IPTV playlists (tens of thousands
        /// of channels) where most channels have no EPG, leaving the guide cluttered with blank rows.
        /// </summary>
        public bool HideChannelsWithoutProgrammes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether channel logos and programme images are
        /// downloaded and cached locally during the guide refresh. When disabled, images are
        /// fetched lazily on first display instead, which dramatically speeds up the guide
        /// refresh for large IPTV playlists (whose logo/image URLs are often dead links that
        /// otherwise block the refresh on network timeouts). Defaults to <c>true</c>.
        /// </summary>
        public bool PreCacheGuideImages { get; set; } = true;
    }
}
