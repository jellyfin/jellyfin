using System;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DirectPlayProfile" />.
    /// </summary>
    public class DirectPlayProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPlayProfile"/> class.
        /// </summary>
        public DirectPlayProfile()
        {
            Container = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPlayProfile"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <param name="audioCodec">The audio codec.</param>
        public DirectPlayProfile(string container, string? videoCodec, string? audioCodec)
        {
            Container = container;
            VideoCodec = videoCodec;
            AudioCodec = audioCodec;
            Type = DlnaProfileType.Video;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPlayProfile"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="audioCodec">The audio codec.</param>
        public DirectPlayProfile(string container, string? audioCodec)
        {
            Container = container;
            AudioCodec = audioCodec;
            Type = DlnaProfileType.Audio;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPlayProfile"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public DirectPlayProfile(string container)
        {
            Container = container;
            Type = DlnaProfileType.Photo;
        }

        /// <summary>
        /// Gets or sets the Container.
        /// </summary>
        [XmlAttribute("container")]
        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        [XmlAttribute("audioCodec")]
        public string? AudioCodec { get; set; }

        /// <summary>
        /// Gets or sets the video codec.
        /// </summary>
        [XmlAttribute("videoCodec")]
        public string? VideoCodec { get; set; }

        /// <summary>
        /// Gets or sets the Dlna profile type.
        /// </summary>
        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        /// <summary>
        /// Checks to see if the <paramref name="container"/> is supported.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>True if the container is supported.</returns>
        public bool SupportsContainer(string container)
        {
            return ContainerProfile.ContainsContainer(Container, container);
        }

        /// <summary>
        /// Checks to see if <paramref name="codec"/> is supported.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>True if the codes is supported.</returns>
        public bool SupportsVideoCodec(string? codec)
        {
            return codec != null && Type == DlnaProfileType.Video && ContainerProfile.ContainsContainer(VideoCodec, codec);
        }

        /// <summary>
        /// Checks to see if <paramref name="codec"/> is supported.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>True if the codes is supported.</returns>
        public bool SupportsAudioCodec(string? codec)
        {
            return (codec != null)
                && (Type == DlnaProfileType.Audio || Type == DlnaProfileType.Video)
                    && ContainerProfile.ContainsContainer(AudioCodec ?? string.Empty, codec);
        }
    }
}
