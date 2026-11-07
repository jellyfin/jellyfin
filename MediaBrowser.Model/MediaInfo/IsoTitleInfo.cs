namespace MediaBrowser.Model.MediaInfo;

/// <summary>
/// Represents a playback title available within an ISO disc image.
/// </summary>
public class IsoTitleInfo
{
    /// <summary>
    /// Gets or sets the 1-based title number.
    /// </summary>
    public int TitleNumber { get; set; }

    /// <summary>
    /// Gets or sets the duration of the title in ticks, if known.
    /// </summary>
    public long? DurationTicks { get; set; }

    /// <summary>
    /// Gets or sets the number of chapters (PTT entries) in this title.
    /// 0 if not known.
    /// </summary>
    public int ChapterCount { get; set; }

    /// <summary>
    /// Gets or sets the number of angles available for this title.
    /// 0 if not known.
    /// </summary>
    public int AngleCount { get; set; }
}
