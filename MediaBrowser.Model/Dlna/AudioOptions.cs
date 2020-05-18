#pragma warning disable CS1591
#pragma warning disable CA1819 // Properties should not return arrays

using System;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Class AudioOptions.
    /// </summary>
    public class AudioOptions
    {
        public AudioOptions()
        {
            Context = EncodingContext.Streaming;

            EnableDirectPlay = true;
            EnableDirectStream = true;
        }

        public bool EnableDirectPlay { get; set; }

        public bool EnableDirectStream { get; set; }

        public bool ForceDirectPlay { get; set; }

        public bool ForceDirectStream { get; set; }

        public Guid ItemId { get; set; }

        public MediaSourceInfo[] MediaSources { get; set; }

        public DeviceProfile Profile { get; set; }

        /// <summary>
        /// Gets or sets media source id. Only needed if a specific AudioStreamIndex or SubtitleStreamIndex are requested.
        /// </summary>
        public string MediaSourceId { get; set; }

        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets override of supported number of audio channels
        /// Example: DeviceProfile supports five channel, but user only has stereo speakers.
        /// </summary>
        public int? MaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the application's configured quality setting.
        /// </summary>
        public long? MaxBitrate { get; set; }

        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        /// <value>The context.</value>
        public EncodingContext Context { get; set; }

        /// <summary>
        /// Gets or sets the audio transcoding bitrate.
        /// </summary>
        /// <value>The audio transcoding bitrate.</value>
        public int? AudioTranscodingBitrate { get; set; }

        /// <summary>
        /// Gets the maximum bitrate.
        /// </summary>
        /// <param name="isAudio">is audio.</param>
        /// <returns>System.Nullable&lt;System.Int32&gt;.</returns>
        public long? CalculateMaxBitrate(bool isAudio)
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
