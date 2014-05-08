namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class StreamOptions
    /// </summary>
    public class StreamOptions
    {
        /// <summary>
        /// Gets or sets the audio bit rate.
        /// </summary>
        /// <value>The audio bit rate.</value>
        public int? AudioBitRate { get; set; }

        /// <summary>
        /// Gets or sets the audio codec.
        /// Omit to copy the original stream
        /// </summary>
        /// <value>The audio encoding format.</value>
        public string AudioCodec { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the max audio channels.
        /// </summary>
        /// <value>The max audio channels.</value>
        public int? MaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the max audio sample rate.
        /// </summary>
        /// <value>The max audio sample rate.</value>
        public int? MaxAudioSampleRate { get; set; }

        /// <summary>
        /// Gets or sets the start time ticks.
        /// </summary>
        /// <value>The start time ticks.</value>
        public long? StartTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the original media should be served statically
        /// Only used with progressive streaming
        /// </summary>
        /// <value><c>true</c> if static; otherwise, <c>false</c>.</value>
        public bool? Static { get; set; }

        /// <summary>
        /// Gets or sets the output file extension.
        /// </summary>
        /// <value>The output file extension.</value>
        public string OutputFileExtension { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        public string DeviceId { get; set; }
    }
}
