namespace MediaBrowser.MediaEncoding.Probing;

/// <summary>
/// FFmpeg Codec Type.
/// </summary>
public enum CodecType
{
    /// <summary>
    /// Video.
    /// </summary>
    Video,

    /// <summary>
    /// Audio.
    /// </summary>
    Audio,

    /// <summary>
    /// Opaque data information usually continuous.
    /// </summary>
    Data,

    /// <summary>
    /// Subtitles.
    /// </summary>
    Subtitle,

    /// <summary>
    /// Opaque data information usually sparse.
    /// </summary>
    Attachment
}
