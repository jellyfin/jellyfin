namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// LyricLineCue model, holds information about the timing of words within a LyricLine.
/// </summary>
public class LyricLineCue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LyricLineCue"/> class.
    /// </summary>
    /// <param name="position">The start character index of the cue.</param>
    /// <param name="endPosition">The end character index of the cue.</param>
    /// <param name="start">The start of the timestamp the lyric is synced to in ticks.</param>
    /// <param name="end">The end of the timestamp the lyric is synced to in ticks.</param>
    public LyricLineCue(int position, int endPosition, long start, long? end)
    {
        Position = position;
        EndPosition = endPosition;
        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the start character index of the cue.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Gets the end character index of the cue.
    /// </summary>
    public int EndPosition { get; }

    /// <summary>
    /// Gets the timestamp the lyric is synced to in ticks.
    /// </summary>
    public long Start { get; }

    /// <summary>
    /// Gets the end timestamp the lyric is synced to in ticks.
    /// </summary>
    public long? End { get; }
}
