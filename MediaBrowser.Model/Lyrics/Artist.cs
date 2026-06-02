namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// Artist information referenced by lyrics.
/// </summary>
public class Artist
{
    /// <summary>
    /// Gets or sets the artist id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artist type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
