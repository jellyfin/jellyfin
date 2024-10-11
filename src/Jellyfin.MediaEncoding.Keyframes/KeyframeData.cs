using System.Collections.Generic;

namespace Jellyfin.MediaEncoding.Keyframes;

/// <summary>
/// Keyframe information for a specific file.
/// </summary>
public class KeyframeData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyframeData"/> class.
    /// </summary>
    /// <param name="totalDuration">The total duration of the video stream in ticks.</param>
    /// <param name="keyframeTicks">The video keyframes in ticks.</param>
    public KeyframeData(long totalDuration, IReadOnlyList<long> keyframeTicks)
    {
        TotalDuration = totalDuration;
        KeyframeTicks = keyframeTicks;
    }

    /// <summary>
    /// Gets the total duration of the stream in ticks.
    /// </summary>
    public long TotalDuration { get; }

    /// <summary>
    /// Gets the keyframes in ticks.
    /// </summary>
    public IReadOnlyList<long> KeyframeTicks { get; }
}
