#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CS1591

using System;
using System.ComponentModel;

namespace MediaBrowser.Model.Configuration
{
    public class LibraryOptions
    {
        private static readonly string[] _defaultTagDelimiters = ["/", "|", ";", "\\"];

        public LibraryOptions()
        {
            TypeOptions = [];
            DisabledSubtitleFetchers = [];
            DisabledMediaSegmentProviders = [];
            MediaSegmentProviderOrder = [];
            SubtitleFetcherOrder = [];
            DisabledLocalMetadataReaders = [];
            DisabledLyricFetchers = [];
            LyricFetcherOrder = [];

            SkipSubtitlesIfAudioTrackMatches = true;
            RequirePerfectSubtitleMatch = true;
            AllowEmbeddedSubtitles = EmbeddedSubtitleOptions.AllowAll;

            AutomaticallyAddToCollection = false;
            EnablePhotos = true;
            SaveSubtitlesWithMedia = true;
            SaveLyricsWithMedia = false;
            SaveTrickplayWithMedia = false;
            PathInfos = [];
            EnableAutomaticSeriesGrouping = true;
            SeasonZeroDisplayName = "Specials";

            PreferNonstandardArtistsTag = false;
            UseCustomTagDelimiters = false;
            CustomTagDelimiters = _defaultTagDelimiters;
            DelimiterWhitelist = [];
        }

        public bool Enabled { get; set; } = true;

        public bool EnablePhotos { get; set; }

        public bool EnableRealtimeMonitor { get; set; }

        public bool EnableLUFSScan { get; set; }

        public bool EnableChapterImageExtraction { get; set; }

        public bool ExtractChapterImagesDuringLibraryScan { get; set; }

        public bool EnableTrickplayImageExtraction { get; set; }

        public bool ExtractTrickplayImagesDuringLibraryScan { get; set; }

        public MediaPathInfo[] PathInfos { get; set; }

        public bool SaveLocalMetadata { get; set; }

        [Obsolete("Disable remote providers in TypeOptions instead")]
        public bool EnableInternetProviders { get; set; }

        public bool EnableAutomaticSeriesGrouping { get; set; }

        public bool EnableEmbeddedTitles { get; set; }

        public bool EnableEmbeddedExtrasTitles { get; set; }

        public bool EnableEmbeddedEpisodeInfos { get; set; }

        public int AutomaticRefreshIntervalDays { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata language.
        /// </summary>
        /// <value>The preferred metadata language.</value>
        public string? PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the metadata country code.
        /// </summary>
        /// <value>The metadata country code.</value>
        public string? MetadataCountryCode { get; set; }

        public string SeasonZeroDisplayName { get; set; }

        public string[]? MetadataSavers { get; set; }

        public string[] DisabledLocalMetadataReaders { get; set; }

        public string[]? LocalMetadataReaderOrder { get; set; }

        public string[] DisabledSubtitleFetchers { get; set; }

        public string[] SubtitleFetcherOrder { get; set; }

        public string[] DisabledMediaSegmentProviders { get; set; }

        public string[] MediaSegmentProviderOrder { get; set; }

        public bool SkipSubtitlesIfEmbeddedSubtitlesPresent { get; set; }

        public bool SkipSubtitlesIfAudioTrackMatches { get; set; }

        public string[]? SubtitleDownloadLanguages { get; set; }

        public bool RequirePerfectSubtitleMatch { get; set; }

        public bool SaveSubtitlesWithMedia { get; set; }

        [DefaultValue(false)]
        public bool SaveLyricsWithMedia { get; set; }

        [DefaultValue(false)]
        public bool SaveTrickplayWithMedia { get; set; }

        public string[] DisabledLyricFetchers { get; set; }

        public string[] LyricFetcherOrder { get; set; }

        [DefaultValue(false)]
        public bool PreferNonstandardArtistsTag { get; set; }

        [DefaultValue(false)]
        public bool UseCustomTagDelimiters { get; set; }

        public string[] CustomTagDelimiters { get; set; }

        public string[] DelimiterWhitelist { get; set; }

        public bool AutomaticallyAddToCollection { get; set; }

        public EmbeddedSubtitleOptions AllowEmbeddedSubtitles { get; set; }

        public TypeOptions[] TypeOptions { get; set; }

        public TypeOptions? GetTypeOptions(string type)
        {
            foreach (var options in TypeOptions)
            {
                if (string.Equals(options.Type, type, StringComparison.OrdinalIgnoreCase))
                {
                    return options;
                }
            }

            return null;
        }
    }
}
