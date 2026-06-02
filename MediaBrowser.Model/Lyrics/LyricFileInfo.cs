namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// Information about a discovered lyric file.
/// </summary>
public class LyricFileInfo
{
    /// <summary>
    /// Gets or sets the lyric path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this lyric is external.
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this lyric is embedded.
    /// </summary>
    public bool IsEmbedded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this lyric is synced.
    /// </summary>
    public bool IsSynced { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this lyric has syllable-level timing.
    /// </summary>
    public bool HasSyllableTiming { get; set; }

    /// <summary>
    /// Gets or sets the media stream index.
    /// </summary>
    public int? StreamIndex { get; set; }
}
