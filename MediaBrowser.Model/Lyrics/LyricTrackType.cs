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
    /// Phonetic lyric content.
    /// </summary>
    Phonetic,

    /// <summary>
    /// Other lyric content.
    /// </summary>
    Other
}
