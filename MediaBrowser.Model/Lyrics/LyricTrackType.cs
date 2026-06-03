namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// The type of a lyric track.
/// </summary>
public enum LyricTrackType
{
    /// <summary>
    /// Main lyric content.
    /// </summary>
    Main,

    /// <summary>
    /// Translated lyric content.
    /// </summary>
    Translation,

    /// <summary>
    /// Romanized or phonetic lyric content.
    /// </summary>
    Romanization,

    /// <summary>
    /// Other lyric content.
    /// </summary>
    Other
}
