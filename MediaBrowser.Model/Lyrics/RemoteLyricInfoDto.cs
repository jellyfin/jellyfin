namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// The remote lyric info dto.
/// </summary>
public class RemoteLyricInfoDto
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
    /// Gets the lyrics.
    /// </summary>
    public required LyricDto Lyrics { get; init; }
}
