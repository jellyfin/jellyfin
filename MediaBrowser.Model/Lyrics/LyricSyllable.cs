namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// Syllable-level lyric timing.
/// </summary>
public class LyricSyllable
{
    /// <summary>
    /// Gets or sets the syllable text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time in ticks.
    /// </summary>
    public long Start { get; set; }

    /// <summary>
    /// Gets or sets the end time in ticks.
    /// </summary>
    public long? End { get; set; }

    /// <summary>
    /// Gets or sets the phonetic text.
    /// </summary>
    public string? Phonetic { get; set; }
}
