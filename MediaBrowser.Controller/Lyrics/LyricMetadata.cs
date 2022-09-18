namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// LyricMetadata model.
/// </summary>
public class LyricMetadata
{
    /// <summary>
    /// Gets or sets Artist - The song artist.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Gets or sets Album - The album this song is on.
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// Gets or sets Title - The title of the song.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets Author - Creator of the lyric data.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets Length - How long the song is.
    /// </summary>
    public string? Length { get; set; }

    /// <summary>
    /// Gets or sets By - Creator of the LRC file.
    /// </summary>
    public string? By { get; set; }

    /// <summary>
    /// Gets or sets Offset - Offset:+/- Timestamp adjustment in milliseconds.
    /// </summary>
    public string? Offset { get; set; }

    /// <summary>
    /// Gets or sets Creator - The Software used to create the LRC file.
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// Gets or sets Version - The version of the Creator used.
    /// </summary>
    public string? Version { get; set; }
}
