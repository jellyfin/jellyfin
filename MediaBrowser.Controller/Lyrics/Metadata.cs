using System.Collections.Generic;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// Metadata model.
/// </summary>
public class Metadata
{
    /// <summary>
    /// Gets or sets Artist - [ar:The song artist].
    /// </summary>
    public string? Ar { get; set; }

    /// <summary>
    /// Gets or sets Album - [al:The album this song is on].
    /// </summary>
    public string? Al { get; set; }

    /// <summary>
    /// Gets or sets Title - [ti:The title of the song].
    /// </summary>
    public string? Ti { get; set; }

    /// <summary>
    /// Gets or sets Author - [au:Creator of the lyric data].
    /// </summary>
    public string? Au { get; set; }

    /// <summary>
    /// Gets or sets Length - [length:How long the song is].
    /// </summary>
    public string? Length { get; set; }

    /// <summary>
    /// Gets or sets By - [by:Creator of the LRC file].
    /// </summary>
    public string? By { get; set; }

    /// <summary>
    /// Gets or sets Offset - [offsec:+/- Timestamp adjustment in milliseconds].
    /// </summary>
    public string? Offset { get; set; }

    /// <summary>
    /// Gets or sets Creator - [re:The Software used to create the LRC file].
    /// </summary>
    public string? Re { get; set; }

    /// <summary>
    /// Gets or sets Version - [ve:The version of the Creator used].
    /// </summary>
    public string? Ve { get; set; }
}
