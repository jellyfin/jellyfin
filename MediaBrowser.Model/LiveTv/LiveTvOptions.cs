#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.LiveTv
{
    public class LiveTvOptions
    {
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

        public LiveTvOptions()
        {
            TunerHosts = Array.Empty<TunerHostInfo>();
            ListingProviders = Array.Empty<ListingsProviderInfo>();
            MediaLocationsCreated = Array.Empty<string>();
            RecordingPostProcessorArguments = "\"{path}\"";
        }
    }

    public class TunerHostInfo
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public string DeviceId { get; set; }
        public string FriendlyName { get; set; }
        public bool ImportFavoritesOnly { get; set; }
        public bool AllowHWTranscoding { get; set; }
        public bool EnableStreamLooping { get; set; }
        public string Source { get; set; }
        public int TunerCount { get; set; }
        public string UserAgent { get; set; }

        public TunerHostInfo()
        {
            AllowHWTranscoding = true;
        }
    }

    public class ListingsProviderInfo
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ListingsId { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public string Path { get; set; }

        public string[] EnabledTuners { get; set; }
        public bool EnableAllTuners { get; set; }
        public string[] NewsCategories { get; set; }
        public string[] SportsCategories { get; set; }
        public string[] KidsCategories { get; set; }
        public string[] MovieCategories { get; set; }
        public NameValuePair[] ChannelMappings { get; set; }
        public string MoviePrefix { get; set; }
        public string PreferredLanguage { get; set; }
        public string UserAgent { get; set; }

        public ListingsProviderInfo()
        {
            NewsCategories = new[] { "news", "journalism", "documentary", "current affairs" };
            SportsCategories = new[] { "sports", "basketball", "baseball", "football" };
            KidsCategories = new[] { "kids", "family", "children", "childrens", "disney" };
            MovieCategories = new[] { "movie" };
            EnabledTuners = Array.Empty<string>();
            EnableAllTuners = true;
            ChannelMappings = Array.Empty<NameValuePair>();
        }
    }
}
