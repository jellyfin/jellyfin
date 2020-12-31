using System;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="AudioOptions" />.
    /// </summary>
    public class AudioOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioOptions"/> class.
        /// </summary>
        /// <param name="itemId">The <see cref="Guid"/>.</param>
        /// <param name="source">An array of <see cref="MediaSourceInfo"/>.</param>
        /// <param name="profile">A <seealso cref="DeviceProfile"/>.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="maxBitRate">The optional maximum bit rate.</param>
        public AudioOptions(Guid itemId, MediaSourceInfo[] source, DeviceProfile profile, string deviceId, int? maxBitRate)
        {
            ItemId = itemId;
            MediaSources = source;
            Profile = profile;
            DeviceId = deviceId;
            MaxBitrate = maxBitRate;
            Context = EncodingContext.Streaming;
            EnableDirectPlay = true;
            EnableDirectStream = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether EnableDirectPlay.
        /// </summary>
        public bool EnableDirectPlay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableDirectStream.
        /// </summary>
        public bool EnableDirectStream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ForceDirectPlay.
        /// </summary>
        public bool ForceDirectPlay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ForceDirectStream.
        /// </summary>
        public bool ForceDirectStream { get; set; }

        /// <summary>
        /// Gets or sets the ItemId.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the MediaSources.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public MediaSourceInfo[] MediaSources { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets the Profile.
        /// </summary>
        public DeviceProfile Profile { get; set; }

        /// <summary>
        /// Gets or sets the MediaSourceId
        /// Optional. Only needed if a specific AudioStreamIndex or SubtitleStreamIndex are requested.
        /// </summary>
        public string? MediaSourceId { get; set; }

        /// <summary>
        /// Gets or sets the DeviceId.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the MaxAudioChannels
        /// Allows an override of supported number of audio channels
        /// Example: DeviceProfile supports five channel, but user only has stereo speakers.
        /// </summary>
        public int? MaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the MaxBitrate
        /// The application's configured quality setting..
        /// </summary>
        public int? MaxBitrate { get; set; }

        /// <summary>
        /// Gets or sets the Context.
        /// </summary>
        public EncodingContext Context { get; set; }

        /// <summary>
        /// Gets or sets the audio transcoding bitrate..
        /// </summary>
        public int? AudioTranscodingBitrate { get; set; }

        /// <summary>
        /// Gets the maximum bitrate.
        /// </summary>
        /// <param name="isAudio">The isAudio<see cref="bool"/>.</param>
        /// <returns>System.Nullable&lt;System.Int32&gt;.</returns>
        public int? GetMaxBitrate(bool isAudio)
        {
            if (MaxBitrate.HasValue)
            {
                return MaxBitrate;
            }

            if (Profile == null)
            {
                return null;
            }

            if (Context == EncodingContext.Static)
            {
                if (isAudio && Profile.MaxStaticMusicBitrate.HasValue)
                {
                    return Profile.MaxStaticMusicBitrate;
                }

                return Profile.MaxStaticBitrate;
            }

            return Profile.MaxStreamingBitrate;
        }
    }
}
