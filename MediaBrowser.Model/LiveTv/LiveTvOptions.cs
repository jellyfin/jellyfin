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
    }
}
