namespace Jellyfin.Data.Entities;

/// <summary>
/// Enum MediaStreamType.
/// </summary>
public enum MediaStreamTypeEntity
{
    /// <summary>
    /// The audio.
    /// </summary>
    Audio = 0,

    /// <summary>
    /// The video.
    /// </summary>
    Video = 1,

    /// <summary>
    /// The subtitle.
    /// </summary>
    Subtitle = 2,

    /// <summary>
    /// The embedded image.
    /// </summary>
    EmbeddedImage = 3,

    /// <summary>
    /// The data.
    /// </summary>
    Data = 4,

    /// <summary>
    /// The lyric.
    /// </summary>
    Lyric = 5
}
