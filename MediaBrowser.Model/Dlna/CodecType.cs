namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// The codec type of a codec profile.
    /// </summary>
    public enum CodecType
    {
        /// <summary>
        /// The profile applies to a video codec.
        /// </summary>
        Video = 0,

        /// <summary>
        /// The profile applies to the audio codec of a video stream.
        /// </summary>
        VideoAudio = 1,

        /// <summary>
        /// The profile applies to an audio codec.
        /// </summary>
        Audio = 2
    }
}
