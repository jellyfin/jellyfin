#pragma warning disable CS1591
#pragma warning disable CA1819

using System;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Represents the server configuration.
/// </summary>
public class ServerConfiguration : BaseApplicationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerConfiguration" /> class.
    /// </summary>
    public ServerConfiguration()
    {
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

    /// <summary>
    /// Gets or sets a value indicating whether to enable prometheus metrics exporting.
    /// </summary>
    public bool EnableMetrics { get; set; } = false;

    public bool EnableNormalizedItemByNameIds { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this instance is port authorized.
    /// </summary>
    /// <value><c>true</c> if this instance is port authorized; otherwise, <c>false</c>.</value>
    public bool IsPortAuthorized { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether quick connect is available for use on this server.
    /// </summary>
    public bool QuickConnectAvailable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether [enable case sensitive item ids].
    /// </summary>
    /// <value><c>true</c> if [enable case sensitive item ids]; otherwise, <c>false</c>.</value>
    public bool EnableCaseSensitiveItemIds { get; set; } = true;

    public bool DisableLiveTvChannelUserDataName { get; set; } = true;

    /// <summary>
    /// Gets or sets the metadata path.
    /// </summary>
    /// <value>The metadata path.</value>
    public string MetadataPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred metadata language.
    /// </summary>
    /// <value>The preferred metadata language.</value>
    public string PreferredMetadataLanguage { get; set; } = "en";

    /// <summary>
    /// Gets or sets the metadata country code.
    /// </summary>
    /// <value>The metadata country code.</value>
    public string MetadataCountryCode { get; set; } = "US";

    /// <summary>
    /// Gets or sets characters to be replaced with a ' ' in strings to create a sort name.
    /// </summary>
    /// <value>The sort replace characters.</value>
    public string[] SortReplaceCharacters { get; set; } = new[] { ".", "+", "%" };

    /// <summary>
    /// Gets or sets characters to be removed from strings to create a sort name.
    /// </summary>
    /// <value>The sort remove characters.</value>
    public string[] SortRemoveCharacters { get; set; } = new[] { ",", "&", "-", "{", "}", "'" };

    /// <summary>
    /// Gets or sets words to be removed from strings to create a sort name.
    /// </summary>
    /// <value>The sort remove words.</value>
    public string[] SortRemoveWords { get; set; } = new[] { "the", "a", "an" };

    /// <summary>
    /// Gets or sets the minimum percentage of an item that must be played in order for playstate to be updated.
    /// </summary>
    /// <value>The min resume PCT.</value>
    public int MinResumePct { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum percentage of an item that can be played while still saving playstate. If this percentage is crossed playstate will be reset to the beginning and the item will be marked watched.
    /// </summary>
    /// <value>The max resume PCT.</value>
    public int MaxResumePct { get; set; } = 90;

    /// <summary>
    /// Gets or sets the minimum duration that an item must have in order to be eligible for playstate updates..
    /// </summary>
    /// <value>The min resume duration seconds.</value>
    public int MinResumeDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the minimum minutes of a book that must be played in order for playstate to be updated.
    /// </summary>
    /// <value>The min resume in minutes.</value>
    public int MinAudiobookResume { get; set; } = 5;

    /// <summary>
    /// Gets or sets the remaining minutes of a book that can be played while still saving playstate. If this percentage is crossed playstate will be reset to the beginning and the item will be marked watched.
    /// </summary>
    /// <value>The remaining time in minutes.</value>
    public int MaxAudiobookResume { get; set; } = 5;

    /// <summary>
    /// Gets or sets the threshold in minutes after a inactive session gets closed automatically.
    /// If set to 0 the check for inactive sessions gets disabled.
    /// </summary>
    /// <value>The close inactive session threshold in minutes. 0 to disable.</value>
    public int InactiveSessionThreshold { get; set; }

    /// <summary>
    /// Gets or sets the delay in seconds that we will wait after a file system change to try and discover what has been added/removed
    /// Some delay is necessary with some items because their creation is not atomic.  It involves the creation of several
    /// different directories and files.
    /// </summary>
    /// <value>The file watcher delay.</value>
    public int LibraryMonitorDelay { get; set; } = 60;

    /// <summary>
    /// Gets or sets the duration in seconds that we will wait after a library updated event before executing the library changed notification.
    /// </summary>
    /// <value>The library update duration.</value>
    public int LibraryUpdateDuration { get; set; } = 30;

    /// <summary>
    /// Gets or sets the image saving convention.
    /// </summary>
    /// <value>The image saving convention.</value>
    public ImageSavingConvention ImageSavingConvention { get; set; }

    public MetadataOptions[] MetadataOptions { get; set; }

    public bool SkipDeserializationForBasicTypes { get; set; } = true;

    public string ServerName { get; set; } = string.Empty;

    public string UICulture { get; set; } = "en-US";

    public bool SaveMetadataHidden { get; set; } = false;

    public NameValuePair[] ContentTypes { get; set; } = Array.Empty<NameValuePair>();

    public int RemoteClientBitrateLimit { get; set; }

    public bool EnableFolderView { get; set; } = false;

    public bool EnableGroupingIntoCollections { get; set; } = false;

    public bool DisplaySpecialsWithinSeasons { get; set; } = true;

    public string[] CodecsUsed { get; set; } = Array.Empty<string>();

    public RepositoryInfo[] PluginRepositories { get; set; } = Array.Empty<RepositoryInfo>();

    public bool EnableExternalContentInSuggestions { get; set; } = true;

    public int ImageExtractionTimeoutMs { get; set; }

    public PathSubstitution[] PathSubstitutions { get; set; } = Array.Empty<PathSubstitution>();

    /// <summary>
    /// Gets or sets a value indicating whether slow server responses should be logged as a warning.
    /// </summary>
    public bool EnableSlowResponseWarning { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold for the slow response time warning in ms.
    /// </summary>
    public long SlowResponseThresholdMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the cors hosts.
    /// </summary>
    public string[] CorsHosts { get; set; } = new[] { "*" };

    /// <summary>
    /// Gets or sets the number of days we should retain activity logs.
    /// </summary>
    public int? ActivityLogRetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the how the library scan fans out.
    /// </summary>
    public int LibraryScanFanoutConcurrency { get; set; }

    /// <summary>
    /// Gets or sets the how many metadata refreshes can run concurrently.
    /// </summary>
    public int LibraryMetadataRefreshConcurrency { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether older plugins should automatically be deleted from the plugin folder.
    /// </summary>
    public bool RemoveOldPlugins { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether clients should be allowed to upload logs.
    /// </summary>
    public bool AllowClientLogUpload { get; set; } = true;

    /// <summary>
    /// Gets or sets the dummy chapter duration in seconds, use 0 (zero) or less to disable generation alltogether.
    /// </summary>
    /// <value>The dummy chapters duration.</value>
    public int DummyChapterDuration { get; set; }

    /// <summary>
    /// Gets or sets the chapter image resolution.
    /// </summary>
    /// <value>The chapter image resolution.</value>
    public ImageResolution ChapterImageResolution { get; set; } = ImageResolution.MatchSource;

    /// <summary>
    /// Gets or sets the limit for parallel image encoding.
    /// </summary>
    /// <value>The limit for parallel image encoding.</value>
    public int ParallelImageEncodingLimit { get; set; }

    /// <summary>
    /// Gets or sets the list of cast receiver applications.
    /// </summary>
    public CastReceiverApplication[] CastReceiverApplications { get; set; } = Array.Empty<CastReceiverApplication>();

    /// <summary>
    /// Gets or sets the trickplay options.
    /// </summary>
    /// <value>The trickplay options.</value>
    public TrickplayOptions TrickplayOptions { get; set; } = new TrickplayOptions();
}
