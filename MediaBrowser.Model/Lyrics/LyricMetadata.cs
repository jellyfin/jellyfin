namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// LyricMetadata model.
/// </summary>
public class LyricMetadata
{
    /// <summary>
    /// Gets or sets the song artist.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Gets or sets the album this song is on.
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// Gets or sets the title of the song.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the author of the lyric data.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the length of the song in ticks.
    /// </summary>
    public long? Length { get; set; }

    /// <summary>
    /// Gets or sets who the LRC file was created by.
    /// </summary>
    public string? By { get; set; }

    /// <summary>
    /// Gets or sets the lyric offset compared to audio in ticks.
    /// </summary>
    public long? Offset { get; set; }

    /// <summary>
    /// Gets or sets the software used to create the LRC file.
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// Gets or sets the version of the creator used.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this lyric is synced.
    /// </summary>
    public bool? IsSynced { get; set; }
}
