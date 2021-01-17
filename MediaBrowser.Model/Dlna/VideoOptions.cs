using System;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="VideoOptions" />.
    /// </summary>
    public class VideoOptions : AudioOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoOptions"/> class.
        /// </summary>
        /// <param name="itemId">The <see cref="Guid"/>.</param>
        /// <param name="source">An array of <see cref="MediaSourceInfo"/>.</param>
        /// <param name="profile">A <seealso cref="DeviceProfile"/>.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="maxBitRate">The optional maximum bit rate.</param>
        public VideoOptions(Guid itemId, MediaSourceInfo[] source, DeviceProfile profile, string deviceId, int? maxBitRate)
        : base(itemId, source, profile, deviceId, maxBitRate)
        {
        }

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
