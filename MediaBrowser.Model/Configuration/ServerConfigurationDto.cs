#pragma warning disable CS1591
#pragma warning disable CA1819

using System;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Configuration;

/// <summary>
/// The server configuration DTO for API use.
/// </summary>
public class ServerConfigurationDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerConfigurationDto" /> class.
    /// </summary>
    public ServerConfigurationDto()
    {
        LogFileRetentionDays = 3;
        MetadataOptions = new[]
        {
            new MetadataOptions()
            {
                ItemType = "Book"
            },
            new MetadataOptions()
            {
                ItemType = "Movie"
            },
            new MetadataOptions
            {
                ItemType = "MusicVideo",
                DisabledMetadataFetchers = new[] { "The Open Movie Database" },
                DisabledImageFetchers = new[] { "The Open Movie Database" }
            },
            new MetadataOptions
            {
                ItemType = "Series",
            },
            new MetadataOptions
            {
                ItemType = "MusicAlbum",
                DisabledMetadataFetchers = new[] { "TheAudioDB" }
            },
            new MetadataOptions
            {
                ItemType = "MusicArtist",
                DisabledMetadataFetchers = new[] { "TheAudioDB" }
            },
            new MetadataOptions
            {
                ItemType = "BoxSet"
            },
            new MetadataOptions
            {
                ItemType = "Season",
            },
            new MetadataOptions
            {
                ItemType = "Episode",
            }
        };
    }

    public int LogFileRetentionDays { get; set; }

    public bool IsStartupWizardCompleted { get; set; }

    public string? CachePath { get; set; }

    public string? PreviousVersionStr { get; set; }

    public bool EnableMetrics { get; set; } = false;

    public bool EnableNormalizedItemByNameIds { get; set; } = true;

    public bool IsPortAuthorized { get; set; }

    public bool QuickConnectAvailable { get; set; } = true;

    public bool EnableCaseSensitiveItemIds { get; set; } = true;

    public bool DisableLiveTvChannelUserDataName { get; set; } = true;

    public string MetadataPath { get; set; } = string.Empty;

    public string PreferredMetadataLanguage { get; set; } = "en";

    public string MetadataCountryCode { get; set; } = "US";

    public string[] SortReplaceCharacters { get; set; } = new[] { ".", "+", "%" };

    public string[] SortRemoveCharacters { get; set; } = new[] { ",", "&", "-", "{", "}", "'" };

    public string[] SortRemoveWords { get; set; } = new[] { "the", "a", "an" };

    public int MinResumePct { get; set; } = 5;

    public int MaxResumePct { get; set; } = 90;

    public int MinResumeDurationSeconds { get; set; } = 300;

    public int MinAudiobookResume { get; set; } = 5;

    public int MaxAudiobookResume { get; set; } = 5;

    public int InactiveSessionThreshold { get; set; }

    public int LibraryMonitorDelay { get; set; } = 60;

    public int LibraryUpdateDuration { get; set; } = 30;

    public int CacheSize { get; set; } = Environment.ProcessorCount * 100;

    public ImageSavingConvention ImageSavingConvention { get; set; }

    public MetadataOptions[] MetadataOptions { get; set; }

    public bool SkipDeserializationForBasicTypes { get; set; } = true;

    public string ServerName { get; set; } = string.Empty;

    public string UICulture { get; set; } = "en-US";

    public bool SaveMetadataHidden { get; set; } = false;

    public NameValuePair[] ContentTypes { get; set; } = Array.Empty<NameValuePair>();

    public int RemoteClientBitrateLimit { get; set; }

    public bool EnableFolderView { get; set; } = false;

    public bool EnableGroupingMoviesIntoCollections { get; set; } = false;

    public bool EnableGroupingShowsIntoCollections { get; set; } = false;

    public bool DisplaySpecialsWithinSeasons { get; set; } = true;

    public string[] CodecsUsed { get; set; } = Array.Empty<string>();

    public RepositoryInfo[] PluginRepositories { get; set; } = Array.Empty<RepositoryInfo>();

    public bool EnableExternalContentInSuggestions { get; set; } = true;

    public int ImageExtractionTimeoutMs { get; set; }

    public PathSubstitution[] PathSubstitutions { get; set; } = Array.Empty<PathSubstitution>();

    public bool EnableSlowResponseWarning { get; set; } = true;

    public long SlowResponseThresholdMs { get; set; } = 500;

    public string[] CorsHosts { get; set; } = new[] { "*" };

    public int? ActivityLogRetentionDays { get; set; } = 30;

    public int LibraryScanFanoutConcurrency { get; set; }

    public int LibraryMetadataRefreshConcurrency { get; set; }

    public bool AllowClientLogUpload { get; set; } = true;

    public int DummyChapterDuration { get; set; }

    public ImageResolution ChapterImageResolution { get; set; } = ImageResolution.MatchSource;

    public int ParallelImageEncodingLimit { get; set; }

    public CastReceiverApplication[] CastReceiverApplications { get; set; } = Array.Empty<CastReceiverApplication>();

    public TrickplayOptions TrickplayOptions { get; set; } = new TrickplayOptions();

    public bool EnableLegacyAuthorization { get; set; }
}
