#pragma warning disable SA1402
#pragma warning disable CA1819

using System;
using System.Xml.Serialization;

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
        [XmlElement("streamdetails")]
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
            Video = Array.Empty<VideoStreamNfo>();
            Audio = Array.Empty<AudioNfo>();
            Subtitle = Array.Empty<SubtitleNfo>();
        }

        /// <summary>
        /// Gets or sets the video nfo tag.
        /// </summary>
        [XmlElement("video")]
        public VideoStreamNfo[] Video { get; set; }

        /// <summary>
        /// Gets or sets the audio nfo tag.
        /// </summary>
        [XmlElement("audio")]
        public AudioNfo[] Audio { get; set; }

        /// <summary>
        /// Gets or sets the subtitle nfo tag.
        /// </summary>
        [XmlElement("subtitle")]
        public SubtitleNfo[] Subtitle { get; set; }
    }

    /// <summary>
    /// The video nfo tag.
    /// </summary>
    public class VideoStreamNfo
    {
        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        [XmlElement("codec")]
        public string? Codec { get; set; }

        /// <summary>
        /// Gets or sets the 3d format.
        /// </summary>
        [XmlElement("format3d")]
        public string? Format3D { get; set; }
    }

    /// <summary>
    /// The audio nfo tag.
    /// </summary>
    public class AudioNfo
    {
        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        [XmlElement("codec")]
        public string? Codec { get; set; }

        /// <summary>
        /// Gets or sets the audio channels.
        /// </summary>
        [XmlElement("channels")]
        public int? Channels { get; set; }

        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        [XmlElement("language")]
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
        [XmlElement("language")]
        public string? Language { get; set; }
    }
}
