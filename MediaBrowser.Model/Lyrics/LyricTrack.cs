using System.Collections.Generic;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// A single lyric track, such as main lyrics, translation, or romanization.
/// </summary>
public class LyricTrack
{
    /// <summary>
    /// Gets or sets the language of this lyric track.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the track type.
    /// </summary>
    public LyricTrackType Type { get; set; } = LyricTrackType.Main;

    /// <summary>
    /// Gets or sets the lyric lines in this track.
    /// </summary>
    public IReadOnlyList<LyricLine> Lines { get; set; } = [];
}
