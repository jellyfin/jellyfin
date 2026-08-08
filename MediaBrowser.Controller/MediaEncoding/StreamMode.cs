namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// How much work a delivery request actually asks FFmpeg to do. Derived from which output codecs
/// ended up as <c>copy</c>, which is settled by <c>EncodingHelper.TryStreamCopy</c> and can change
/// again once a live stream is opened — so this is computed on demand rather than stored.
/// </summary>
public enum StreamMode
{
    /// <summary>
    /// The video is re-encoded. The expensive case, and the only one that loads the CPU or GPU.
    /// </summary>
    Transcode,

    /// <summary>
    /// The video is copied but the audio is re-encoded, for clients that can play the video as-is
    /// but not its soundtrack.
    /// </summary>
    DirectStream,

    /// <summary>
    /// Every stream is copied and only the container changes. I/O bound and cheap.
    /// </summary>
    Remux
}
