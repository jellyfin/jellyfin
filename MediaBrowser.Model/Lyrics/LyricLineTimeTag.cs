namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// LyricLineTimeTag model.
/// </summary>
public class LyricLineTimeTag
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LyricLineTimeTag"/> class.
    /// </summary>
    /// <param name="position">The start of the character index of the lyric.</param>
    /// <param name="start">The start of the timestamp the lyric is synced to in ticks.</param>
    public LyricLineTimeTag(int position, long start)
    {
        Position = position;
        Start = start;
    }

    /// <summary>
    /// Gets the character index of the lyric.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Gets the timestamp the lyric is synced to in ticks.
    /// </summary>
    public long Start { get; }
}
