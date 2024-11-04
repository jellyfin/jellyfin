namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// TimeTag model.
/// </summary>
public class TimeTag
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeTag"/> class.
    /// </summary>
    /// <param name="position">The character index of the lyric.</param>
    /// <param name="timestamp">The timestamp the lyric is synced to.</param>
    public TimeTag(int position, int? timestamp = null)
    {
        Position = position;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Gets the character index of the lyric.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Gets the timestamp the lyric is synced to.
    /// </summary>
    public int? Timestamp { get; }
}
