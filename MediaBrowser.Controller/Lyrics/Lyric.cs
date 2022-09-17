namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// Lyric model.
/// </summary>
public class Lyric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Lyric"/> class.
    /// </summary>
    /// <param name="start">The lyric start time in ticks.</param>
    /// <param name="text">The lyric text.</param>
    public Lyric(string text, long? start = null)
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
