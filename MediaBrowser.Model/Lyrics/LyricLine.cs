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
    /// <param name="timetags">The Enhanced LRC timestamps for the song.</param>
    public LyricLine(string text, long? start = null, Dictionary<int, int?>? timetags = null)
    {
        Text = text;
        Start = start;
        TimeTags = timetags;
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
    /// Gets the Enhanced LRC timestamps for the song.
    /// </summary>
    public Dictionary<int, int?>? TimeTags { get; }
}
