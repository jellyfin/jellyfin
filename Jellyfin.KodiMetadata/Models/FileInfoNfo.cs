#pragma warning disable SA1402
#pragma warning disable CA1819

using System;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The fileinfo nfo tag.
    /// </summary>
    public class FileInfoNfo
    {
        /// <summary>
        /// Gets or sets the streamdetails nfo tag.
        /// </summary>
        public StreamDetailsNfo? StreamDetails { get; set; }
    }

    /// <summary>
    /// The streamdetails nfo tag.
    /// </summary>
    public class StreamDetailsNfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDetailsNfo"/> class.
        /// </summary>
        public StreamDetailsNfo()
        {
            Video = Array.Empty<VideoNfo>();
            Audio = Array.Empty<AudioNfo>();
            Subtitle = Array.Empty<SubtitleNfo>();
        }

        /// <summary>
        /// Gets or sets the video nfo tag.
        /// </summary>
        public VideoNfo[] Video { get; set; }

        /// <summary>
        /// Gets or sets the audio nfo tag.
        /// </summary>
        public AudioNfo[] Audio { get; set; }

        /// <summary>
        /// Gets or sets the subtitle nfo tag.
        /// </summary>
        public SubtitleNfo[] Subtitle { get; set; }
    }

    /// <summary>
    /// The vidoe nfo tag.
    /// </summary>
    public class VideoNfo
    {
        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        public string? Codec { get; set; }
    }

    /// <summary>
    /// The audio nfo tag.
    /// </summary>
    public class AudioNfo
    {
        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        public string? Codec { get; set; }

        /// <summary>
        /// Gets or sets the audio channels.
        /// </summary>
        public int? Channels { get; set; }

        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        public string? Language { get; set; }
    }

    /// <summary>
    /// The subtitle nfo tag.
    /// </summary>
    public class SubtitleNfo
    {
        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        public string? Language { get; set; }
    }
}
