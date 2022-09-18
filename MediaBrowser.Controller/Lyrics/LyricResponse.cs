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
    public LyricMetadata Metadata { get; set; } = new LyricMetadata();

    /// <summary>
    /// Gets or sets Lyrics.
    /// </summary>
    public IReadOnlyCollection<LyricLine> Lyrics { get; set; } = new List<LyricLine>();
}
