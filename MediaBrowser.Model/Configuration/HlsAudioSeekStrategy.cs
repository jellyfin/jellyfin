namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// An enum representing the options to seek the input audio stream when
    /// transcoding HLS segments.
    /// </summary>
    public enum HlsAudioSeekStrategy
    {
        /// <summary>
        /// When video is transcoded and audio is copied, use a bitstream filter
        /// to drop copied audio packets before the seek point, aligning them
        /// with the accurately-seeked video. Timestamps are accurate and audio
        /// remains stream-copied (no re-encoding overhead).
        /// </summary>
        TrimCopiedAudio = 0,

        /// <summary>
        /// Prevent audio streams from being copied if the video stream is transcoded.
        /// The resulting timestamps will be accurate, but additional audio transcoding
        /// overhead will be incurred.
        /// </summary>
        TranscodeAudio = 1,
    }
}
