namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// An enum representing the options to seek the input audio stream when
    /// transcoding HLS segments.
    /// </summary>
    public enum HlsAudioSeekStrategy
    {
        /// <summary>
        /// If the video stream is transcoded and the audio stream is copied,
        /// seek the video stream to the same keyframe as the audio stream. The
        /// resulting timestamps in the output streams may be inaccurate.
        /// </summary>
        DisableAccurateSeek = 0,

        /// <summary>
        /// Prevent audio streams from being copied if the video stream is transcoded.
        /// The resulting timestamps will be accurate, but additional audio transcoding
        /// overhead will be incurred.
        /// </summary>
        TranscodeAudio = 1,
    }
}
