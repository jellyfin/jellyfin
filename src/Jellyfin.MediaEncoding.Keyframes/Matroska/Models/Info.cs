namespace Jellyfin.MediaEncoding.Keyframes.Matroska.Models;

/// <summary>
/// The matroska Info segment.
/// </summary>
internal class Info
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Info"/> class.
    /// </summary>
    /// <param name="timestampScale">The timestamp scale in nanoseconds.</param>
    /// <param name="duration">The duration of the entire file.</param>
    public Info(long timestampScale, double? duration)
    {
        TimestampScale = timestampScale;
        Duration = duration;
    }

    /// <summary>
    /// Gets the timestamp scale in nanoseconds.
    /// </summary>
    public long TimestampScale { get; }

    /// <summary>
    /// Gets the total duration of the file.
    /// </summary>
    public double? Duration { get; }
}
