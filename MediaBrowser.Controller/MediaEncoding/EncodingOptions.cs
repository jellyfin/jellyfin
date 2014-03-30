using MediaBrowser.Controller.Dlna;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class EncodingOptions
    {
        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the media source identifier.
        /// </summary>
        /// <value>The media source identifier.</value>
        public string MediaSourceId { get; set; }

        /// <summary>
        /// Gets or sets the device profile.
        /// </summary>
        /// <value>The device profile.</value>
        public DeviceProfile DeviceProfile { get; set; }
        
        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        /// <value>The output path.</value>
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        /// <value>The container.</value>
        public string Container { get; set; }
        
        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        /// <value>The audio codec.</value>
        public string AudioCodec { get; set; }
        
        /// <summary>
        /// Gets or sets the start time ticks.
        /// </summary>
        /// <value>The start time ticks.</value>
        public long? StartTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the maximum channels.
        /// </summary>
        /// <value>The maximum channels.</value>
        public int? MaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the channels.
        /// </summary>
        /// <value>The channels.</value>
        public int? AudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the sample rate.
        /// </summary>
        /// <value>The sample rate.</value>
        public int? AudioSampleRate { get; set; }

        /// <summary>
        /// Gets or sets the bit rate.
        /// </summary>
        /// <value>The bit rate.</value>
        public int? AudioBitRate { get; set; }

        /// <summary>
        /// Gets or sets the maximum audio bit rate.
        /// </summary>
        /// <value>The maximum audio bit rate.</value>
        public int? MaxAudioBitRate { get; set; }
    }
}
