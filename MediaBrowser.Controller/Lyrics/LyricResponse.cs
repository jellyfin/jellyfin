using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// LyricResponse model.
/// </summary>
public class LyricResponse
{
    /// <summary>
    /// Gets or sets Metadata.
    /// </summary>
    public LyricMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets Lyrics.
    /// </summary>
    public IReadOnlyList<LyricLine> Lyrics { get; set; } = Array.Empty<LyricLine>();
}
