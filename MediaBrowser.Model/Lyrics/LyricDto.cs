using System.Collections.Generic;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// LyricResponse model.
/// </summary>
public class LyricDto
{
    /// <summary>
    /// Gets or sets Metadata for the lyrics.
    /// </summary>
    public LyricMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets a collection of individual lyric lines.
    /// </summary>
    public IReadOnlyList<LyricLine> Lyrics { get; set; } = [];
}
