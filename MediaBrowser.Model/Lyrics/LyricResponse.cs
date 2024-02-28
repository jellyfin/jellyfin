using System.IO;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// LyricResponse model.
/// </summary>
public class LyricResponse
{
    /// <summary>
    /// Gets or sets the lyric stream.
    /// </summary>
    public required Stream Stream { get; set; }

    /// <summary>
    /// Gets or sets the lyric format.
    /// </summary>
    public required string Format { get; set; }
}
