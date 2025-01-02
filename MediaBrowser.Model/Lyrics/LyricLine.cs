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
    /// <param name="cues">The time-aligned cues for the song's lyrics.</param>
    public LyricLine(string text, long? start = null, IReadOnlyList<LyricLineCue>? cues = null)
    {
        Text = text;
        Start = start;
        Cues = cues;
    }

    /// <summary>
    /// Gets the text of this lyric line.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the start time in ticks.
    /// </summary>
    public long? Start { get; }

    /// <summary>
    /// Gets the time-aligned cues for the song's lyrics.
    /// </summary>
    public IReadOnlyList<LyricLineCue>? Cues { get; }
}
