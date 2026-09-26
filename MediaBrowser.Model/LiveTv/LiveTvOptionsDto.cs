#pragma warning disable CA1819

using System;

namespace MediaBrowser.Model.LiveTv;

/// <summary>
/// The LiveTV options DTO for API use.
/// </summary>
public class LiveTvOptionsDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiveTvOptionsDto" /> class.
    /// </summary>
    public LiveTvOptionsDto()
    {
        TunerHosts = Array.Empty<TunerHostInfo>();
        ListingProviders = Array.Empty<ListingsProviderInfo>();
        MediaLocationsCreated = Array.Empty<string>();
        RecordingPostProcessorArguments = "\"{path}\"";
    }

    /// <summary>
    /// Gets or sets the guide days.
    /// </summary>
    public int? GuideDays { get; set; }

    /// <summary>
    /// Gets or sets the recording path.
    /// </summary>
    public string? RecordingPath { get; set; }

    /// <summary>
    /// Gets or sets the movie recording path.
    /// </summary>
    public string? MovieRecordingPath { get; set; }

    /// <summary>
    /// Gets or sets the series recording path.
    /// </summary>
    public string? SeriesRecordingPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable recording subfolders.
    /// </summary>
    public bool EnableRecordingSubfolders { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable original audio with encoded recordings.
    /// </summary>
    public bool EnableOriginalAudioWithEncodedRecordings { get; set; }

    /// <summary>
    /// Gets or sets the tuner hosts.
    /// </summary>
    public TunerHostInfo[] TunerHosts { get; set; }

    /// <summary>
    /// Gets or sets the listing providers.
    /// </summary>
    public ListingsProviderInfo[] ListingProviders { get; set; }

    /// <summary>
    /// Gets or sets the pre-padding seconds.
    /// </summary>
    public int PrePaddingSeconds { get; set; }

    /// <summary>
    /// Gets or sets the post-padding seconds.
    /// </summary>
    public int PostPaddingSeconds { get; set; }

    /// <summary>
    /// Gets or sets the media locations created.
    /// </summary>
    public string[] MediaLocationsCreated { get; set; }

    /// <summary>
    /// Gets or sets the recording post processor.
    /// </summary>
    public string? RecordingPostProcessor { get; set; }

    /// <summary>
    /// Gets or sets the recording post processor arguments.
    /// </summary>
    public string RecordingPostProcessorArguments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to save recording NFO.
    /// </summary>
    public bool SaveRecordingNFO { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to save recording images.
    /// </summary>
    public bool SaveRecordingImages { get; set; } = true;
}
