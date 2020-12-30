namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="VideoOptions" />.
    /// </summary>
    public class VideoOptions : AudioOptions
    {
        /// <summary>
        /// Gets or sets the audio stream index.
        /// </summary>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the subtitle stream index.
        /// </summary>
        public int? SubtitleStreamIndex { get; set; }
    }
}
