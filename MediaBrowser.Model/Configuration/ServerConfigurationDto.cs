#pragma warning disable CS1591
#pragma warning disable CA1819

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
    public int LogFileRetentionDays { get; set; }

    public bool IsStartupWizardCompleted { get; set; }

    public string? CachePath { get; set; }

    public string? PreviousVersionStr { get; set; }

    public bool EnableMetrics { get; set; }

    public bool EnableNormalizedItemByNameIds { get; set; }

    public bool IsPortAuthorized { get; set; }

    public bool QuickConnectAvailable { get; set; }

    public bool EnableCaseSensitiveItemIds { get; set; }

    public bool DisableLiveTvChannelUserDataName { get; set; }

    public string MetadataPath { get; set; } = null!;

    public string PreferredMetadataLanguage { get; set; } = null!;

    public string MetadataCountryCode { get; set; } = null!;

    public string[] SortReplaceCharacters { get; set; } = null!;

    public string[] SortRemoveCharacters { get; set; } = null!;

    public string[] SortRemoveWords { get; set; } = null!;

    public int MinResumePct { get; set; }

    public int MaxResumePct { get; set; }

    public int MinResumeDurationSeconds { get; set; }

    public int MinAudiobookResume { get; set; }

    public int MaxAudiobookResume { get; set; }

    public int InactiveSessionThreshold { get; set; }

    public int LibraryMonitorDelay { get; set; }

    public int LibraryUpdateDuration { get; set; }

    public int CacheSize { get; set; }

    public ImageSavingConvention ImageSavingConvention { get; set; }

    public MetadataOptions[] MetadataOptions { get; set; } = null!;

    public bool SkipDeserializationForBasicTypes { get; set; }

    public string ServerName { get; set; } = null!;

    public string UICulture { get; set; } = null!;

    public bool SaveMetadataHidden { get; set; }

    public NameValuePair[] ContentTypes { get; set; } = null!;

    public int RemoteClientBitrateLimit { get; set; }

    public bool EnableFolderView { get; set; }

    public bool EnableGroupingMoviesIntoCollections { get; set; }

    public bool EnableGroupingShowsIntoCollections { get; set; }

    public bool DisplaySpecialsWithinSeasons { get; set; }

    public string[] CodecsUsed { get; set; } = null!;

    public RepositoryInfo[] PluginRepositories { get; set; } = null!;

    public bool EnableExternalContentInSuggestions { get; set; }

    public int ImageExtractionTimeoutMs { get; set; }

    public PathSubstitution[] PathSubstitutions { get; set; } = null!;

    public bool EnableSlowResponseWarning { get; set; }

    public long SlowResponseThresholdMs { get; set; }

    public string[] CorsHosts { get; set; } = null!;

    public int? ActivityLogRetentionDays { get; set; }

    public int LibraryScanFanoutConcurrency { get; set; }

    public int LibraryMetadataRefreshConcurrency { get; set; }

    public bool AllowClientLogUpload { get; set; }

    public int DummyChapterDuration { get; set; }

    public ImageResolution ChapterImageResolution { get; set; }

    public int ParallelImageEncodingLimit { get; set; }

    public CastReceiverApplication[] CastReceiverApplications { get; set; } = null!;

    public TrickplayOptions TrickplayOptions { get; set; } = null!;

    public bool EnableLegacyAuthorization { get; set; }
}
