using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Model.LiveTv
{
    public class LiveTvOptions
    {
        public int? GuideDays { get; set; }
        public bool EnableMovieProviders { get; set; }
        public string RecordingPath { get; set; }
        public string MovieRecordingPath { get; set; }
        public string SeriesRecordingPath { get; set; }
        public bool EnableAutoOrganize { get; set; }
        public bool EnableRecordingEncoding { get; set; }
        public bool EnableRecordingSubfolders { get; set; }
        public bool EnableOriginalAudioWithEncodedRecordings { get; set; }

        public List<TunerHostInfo> TunerHosts { get; set; }
        public List<ListingsProviderInfo> ListingProviders { get; set; }

        public int PrePaddingSeconds { get; set; }
        public int PostPaddingSeconds { get; set; }

        public string[] MediaLocationsCreated { get; set; }

        public LiveTvOptions()
        {
            EnableMovieProviders = true;
            EnableRecordingSubfolders = true;
            TunerHosts = new List<TunerHostInfo>();
            ListingProviders = new List<ListingsProviderInfo>();
            MediaLocationsCreated = new string[] { };
        }
    }

    public class TunerHostInfo
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public string DeviceId { get; set; }
        public bool ImportFavoritesOnly { get; set; }
        public bool AllowHWTranscoding { get; set; }
        public bool IsEnabled { get; set; }
        public string M3UUrl { get; set; }
        public string InfoUrl { get; set; }
        public string FriendlyName { get; set; }
        public int Tuners { get; set; }
        public string DiseqC { get; set; }
        public string SourceA { get; set; }
        public string SourceB { get; set; }
        public string SourceC { get; set; }
        public string SourceD { get; set; }

        public int DataVersion { get; set; }

        public TunerHostInfo()
        {
            IsEnabled = true;
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

        public string GetMappedChannel(string channelNumber)
        {
            foreach (NameValuePair mapping in ChannelMappings)
            {
                if (StringHelper.EqualsIgnoreCase(mapping.Name, channelNumber))
                {
                    return mapping.Value;
                }
            }
            return channelNumber;
        }
    }
}
