namespace MediaBrowser.Controller.Lyrics;

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
        Start = start;
        Text = text;
    }

    /// <summary>
    /// Gets the start time in ticks.
    /// </summary>
    public long? Start { get; }

    /// <summary>
    /// Gets the text.
    /// </summary>
    public string Text { get; }
}
