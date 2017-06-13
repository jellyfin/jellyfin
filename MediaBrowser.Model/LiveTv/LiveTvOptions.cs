using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.LiveTv
{
    public class LiveTvOptions
    {
        public int? GuideDays { get; set; }
        public string RecordingPath { get; set; }
        public string MovieRecordingPath { get; set; }
        public string SeriesRecordingPath { get; set; }
        public bool EnableRecordingEncoding { get; set; }
        public string RecordingEncodingFormat { get; set; }
        public bool EnableRecordingSubfolders { get; set; }
        public bool EnableOriginalAudioWithEncodedRecordings { get; set; }
        public string RecordedVideoCodec { get; set; }

        public List<TunerHostInfo> TunerHosts { get; set; }
        public List<ListingsProviderInfo> ListingProviders { get; set; }

        public int PrePaddingSeconds { get; set; }
        public int PostPaddingSeconds { get; set; }

        public string[] MediaLocationsCreated { get; set; }

        public string RecordingPostProcessor { get; set; }
        public string RecordingPostProcessorArguments { get; set; }

        public LiveTvOptions()
        {
            TunerHosts = new List<TunerHostInfo>();
            ListingProviders = new List<ListingsProviderInfo>();
            MediaLocationsCreated = new string[] { };
            RecordingEncodingFormat = "mkv";
            RecordingPostProcessorArguments = "\"{path}\"";
            EnableRecordingEncoding = true;
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
        public bool EnableTvgId { get; set; }

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
        public bool EnableNewProgramIds { get; set; }

        public ListingsProviderInfo()
        {
            NewsCategories = new string[] { "news", "journalism", "documentary", "current affairs" };
            SportsCategories = new string[] { "sports", "basketball", "baseball", "football" };
            KidsCategories = new string[] { "kids", "family", "children", "childrens", "disney" };
            MovieCategories = new string[] { "movie" };
            EnabledTuners = new string[] { };
            EnableAllTuners = true;
            ChannelMappings = new NameValuePair[] {};
        }
    }
}
