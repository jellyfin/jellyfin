namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class VideoStreamOptions
    /// </summary>
    public class VideoStreamOptions : StreamOptions
    {
        /// <summary>
        /// Gets or sets the video codec.
        /// Omit to copy
        /// </summary>
        /// <value>The video codec.</value>
        public string VideoCodec { get; set; }

        /// <summary>
        /// Gets or sets the video bit rate.
        /// </summary>
        /// <value>The video bit rate.</value>
        public int? VideoBitRate { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the width of the max.
        /// </summary>
        /// <value>The width of the max.</value>
        public int? MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the max.
        /// </summary>
        /// <value>The height of the max.</value>
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the frame rate.
        /// </summary>
        /// <value>The frame rate.</value>
        public double? FrameRate { get; set; }

        /// <summary>
        /// Gets or sets the index of the audio stream.
        /// </summary>
        /// <value>The index of the audio stream.</value>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the video stream.
        /// </summary>
        /// <value>The index of the video stream.</value>
        public int? VideoStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the subtitle stream.
        /// </summary>
        /// <value>The index of the subtitle stream.</value>
        public int? SubtitleStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        public string Level { get; set; }

        /// <summary>
        /// Gets or sets the baseline stream audio bit rate.
        /// </summary>
        /// <value>The baseline stream audio bit rate.</value>
        public int? BaselineStreamAudioBitRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [append baseline stream].
        /// </summary>
        /// <value><c>true</c> if [append baseline stream]; otherwise, <c>false</c>.</value>
        public bool AppendBaselineStream { get; set; }

        /// <summary>
        /// Gets or sets the time stamp offset ms. Only used with HLS.
        /// </summary>
        /// <value>The time stamp offset ms.</value>
        public int? TimeStampOffsetMs { get; set; }
    }
}