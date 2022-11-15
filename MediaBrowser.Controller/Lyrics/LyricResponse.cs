using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// LyricResponse model.
/// </summary>
public class LyricResponse
{
    /// <summary>
    /// Gets or sets Metadata for the lyrics.
    /// </summary>
    public LyricMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets a collection of individual lyric lines.
    /// </summary>
    public IReadOnlyList<LyricLine> Lyrics { get; set; } = Array.Empty<LyricLine>();
}
