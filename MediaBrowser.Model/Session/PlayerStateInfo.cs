#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Session
{
    public class PlayerStateInfo
    {
        private float? _playbackSpeed = 1.0f;

        /// <summary>
        /// Gets or sets the now playing position ticks.
        /// </summary>
        /// <value>The now playing position ticks.</value>
        public long? PositionTicks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can seek.
        /// </summary>
        /// <value><c>true</c> if this instance can seek; otherwise, <c>false</c>.</value>
        public bool CanSeek { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is paused.
        /// </summary>
        /// <value><c>true</c> if this instance is paused; otherwise, <c>false</c>.</value>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is muted.
        /// </summary>
        /// <value><c>true</c> if this instance is muted; otherwise, <c>false</c>.</value>
        public bool IsMuted { get; set; }

        /// <summary>
        /// Gets or sets the volume level.
        /// </summary>
        /// <value>The volume level.</value>
        public int? VolumeLevel { get; set; }

        /// <summary>
        /// Gets or sets the index of the now playing audio stream.
        /// </summary>
        /// <value>The index of the now playing audio stream.</value>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the now playing subtitle stream.
        /// </summary>
        /// <value>The index of the now playing subtitle stream.</value>
        public int? SubtitleStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the now playing media version identifier.
        /// </summary>
        /// <value>The now playing media version identifier.</value>
        public string MediaSourceId { get; set; }

        /// <summary>
        /// Gets or sets the play method.
        /// </summary>
        /// <value>The play method.</value>
        public PlayMethod? PlayMethod { get; set; }

        /// <summary>
        /// Gets or sets the repeat mode.
        /// </summary>
        /// <value>The repeat mode.</value>
        public RepeatMode RepeatMode { get; set; }

        /// <summary>
        /// Gets or sets the playback order.
        /// </summary>
        /// <value>The playback order.</value>
        public PlaybackOrder PlaybackOrder { get; set; }

        /// <summary>
        /// Gets or sets the now playing live stream identifier.
        /// </summary>
        /// <value>The live stream identifier.</value>
        public string LiveStreamId { get; set; }

        /// <summary>
        /// Gets or sets the playback speed.
        /// </summary>
        /// <value>The playback speed.</value>
        public float? PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = value is null ? null : (float?)Math.Round(Math.Clamp(value.Value, 0.1f, 10.0f), 1);
        }
    }
}
