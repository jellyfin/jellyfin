using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Model.Providers;

/// <summary>
/// The remote lyric info.
/// </summary>
public class RemoteLyricInfo
{
    /// <summary>
    /// Gets or sets the id for the lyric.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Gets the lyric metadata.
    /// </summary>
    public required LyricMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the lyrics.
    /// </summary>
    public required LyricResponse Lyrics { get; init; }
}
