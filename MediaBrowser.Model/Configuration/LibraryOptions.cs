#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MediaBrowser.Model.Configuration
{
    public class LibraryOptions
    {
        private ICollection<TypeOptions> _typeOptions;
        private ICollection<string>? _localMetadataReaderOrder = [];
        private ICollection<MediaPathInfo> _pathInfos = [];

        public LibraryOptions()
        {
            _typeOptions = Array.Empty<TypeOptions>();
            DisabledSubtitleFetchers = [];
            SubtitleDownloadLanguages = [];
            SubtitleFetcherOrder = Array.Empty<string>();
            DisabledLocalMetadataReaders = Array.Empty<string>();
            MetadataSavers = [];
            SkipSubtitlesIfAudioTrackMatches = true;
            RequirePerfectSubtitleMatch = true;
            AllowEmbeddedSubtitles = EmbeddedSubtitleOptions.AllowAll;

            AutomaticallyAddToCollection = false;
            EnablePhotos = true;
            SaveSubtitlesWithMedia = true;
            SaveLyricsWithMedia = true;
            _pathInfos = Array.Empty<MediaPathInfo>();
            EnableAutomaticSeriesGrouping = true;
            SeasonZeroDisplayName = "Specials";
        }

        public bool Enabled { get; set; } = true;

        public bool EnablePhotos { get; set; }

        public bool EnableRealtimeMonitor { get; set; }

        public bool EnableLUFSScan { get; set; }

        public bool EnableChapterImageExtraction { get; set; }

        public bool ExtractChapterImagesDuringLibraryScan { get; set; }

        public bool EnableTrickplayImageExtraction { get; set; }

        public bool ExtractTrickplayImagesDuringLibraryScan { get; set; }

        public ICollection<MediaPathInfo> PathInfos
        {
            get => _pathInfos;
        }

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

        public ICollection<string>? MetadataSavers { get;  }

        public ICollection<string> DisabledLocalMetadataReaders { get;  }

        public ICollection<string>? LocalMetadataReaderOrder
        {
            get => _localMetadataReaderOrder;
        }

        public ICollection<string> DisabledSubtitleFetchers { get;  }

        public ICollection<string> SubtitleFetcherOrder { get;  }

        public bool SkipSubtitlesIfEmbeddedSubtitlesPresent { get; set; }

        public bool SkipSubtitlesIfAudioTrackMatches { get; set; }

        public ICollection<string> SubtitleDownloadLanguages { get; }

        public bool RequirePerfectSubtitleMatch { get; set; }

        public bool SaveSubtitlesWithMedia { get; set; }

        [DefaultValue(true)]
        public bool SaveLyricsWithMedia { get; set; }

        public bool AutomaticallyAddToCollection { get; set; }

        public EmbeddedSubtitleOptions AllowEmbeddedSubtitles { get; set; }

        public ICollection<TypeOptions> TypeOptions
        {
            get => _typeOptions;
        }

        public TypeOptions? GetTypeOption(string type)
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

        public void SetTypeOptions(ICollection<TypeOptions> typeOptions)
        {
            _typeOptions = typeOptions;
        }

        public void SetLocalMetadataReaderOrder(string[]? localMetadataReaderOrder)
        {
            _localMetadataReaderOrder = localMetadataReaderOrder ?? [];
        }

        public void SetPathInfos(MediaPathInfo[] pathInfos)
        {
            _pathInfos = new List<MediaPathInfo>(pathInfos);
        }
    }
}
