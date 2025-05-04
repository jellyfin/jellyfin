using System;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Class MediaOptions.
    /// </summary>
    public class MediaOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOptions"/> class.
        /// </summary>
        public MediaOptions()
        {
            Context = EncodingContext.Streaming;

            EnableDirectPlay = true;
            EnableDirectStream = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether direct playback is allowed.
        /// </summary>
        public bool EnableDirectPlay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether direct streaming is allowed.
        /// </summary>
        public bool EnableDirectStream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether direct playback is forced.
        /// </summary>
        public bool ForceDirectPlay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether direct streaming is forced.
        /// </summary>
        public bool ForceDirectStream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether audio stream copy is allowed.
        /// </summary>
        public bool AllowAudioStreamCopy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether video stream copy is allowed.
        /// </summary>
        public bool AllowVideoStreamCopy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether always burn in subtitles when transcoding.
        /// </summary>
        public bool AlwaysBurnInSubtitleWhenTranscoding { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the media sources.
        /// </summary>
        public MediaSourceInfo[] MediaSources { get; set; } = Array.Empty<MediaSourceInfo>();

        /// <summary>
        /// Gets or sets the device profile.
        /// </summary>
        public required DeviceProfile Profile { get; set; }

        /// <summary>
        /// Gets or sets a media source id. Optional. Only needed if a specific AudioStreamIndex or SubtitleStreamIndex are requested.
        /// </summary>
        public string? MediaSourceId { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets an override of supported number of audio channels
        /// Example: DeviceProfile supports five channel, but user only has stereo speakers.
        /// </summary>
        public int? MaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the application's configured maximum bitrate.
        /// </summary>
        public int? MaxBitrate { get; set; }

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
        /// Gets or sets an override for the audio stream index.
        /// </summary>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets an override for the subtitle stream index.
        /// </summary>
        public int? SubtitleStreamIndex { get; set; }

        /// <summary>
        /// Gets the maximum bitrate.
        /// </summary>
        /// <param name="isAudio">Whether or not this is audio.</param>
        /// <returns>System.Nullable&lt;System.Int32&gt;.</returns>
        public int? GetMaxBitrate(bool isAudio)
        {
            if (MaxBitrate.HasValue)
            {
                return MaxBitrate;
            }

            if (Profile is null)
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
