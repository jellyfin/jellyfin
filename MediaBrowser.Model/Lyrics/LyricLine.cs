using System.Collections.Generic;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// Lyric model.
/// </summary>
public class LyricLine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LyricLine"/> class.
    /// </summary>
    /// <param name="text">The lyric text.</param>
    /// <param name="start">The lyric start time in ticks.</param>
    public LyricLine(string text, long? start = null)
    {
        Text = text;
        Start = start;
    }

    /// <summary>
    /// Gets or sets the text of this lyric line.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the start time in ticks.
    /// </summary>
    public long? Start { get; set; }

    /// <summary>
    /// Gets or sets the end time in ticks.
    /// </summary>
    public long? End { get; set; }

    /// <summary>
    /// Gets the duration in ticks.
    /// </summary>
    public long? Duration => Start.HasValue && End.HasValue && End.Value >= Start.Value ? End.Value - Start.Value : null;

    /// <summary>
    /// Gets or sets the ids of artists associated with this lyric line.
    /// </summary>
    public IReadOnlyList<string> ArtistIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the syllable-level timing for this lyric line.
    /// </summary>
    public IReadOnlyList<LyricSyllable> Syllables { get; set; } = [];
}
