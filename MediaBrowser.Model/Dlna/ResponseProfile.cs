using System;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ResponseProfile" />.
    /// </summary>
    public class ResponseProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseProfile"/> class.
        /// </summary>
        public ResponseProfile()
        {
            Conditions = Array.Empty<ProfileCondition>();
            MimeType = string.Empty;
            Container = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseProfile"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="type">The <see cref="DlnaProfileType"/>.</param>
        /// <param name="mimetype">The mime type.</param>
        public ResponseProfile(string container, DlnaProfileType type, string? mimetype)
        {
            Conditions = Array.Empty<ProfileCondition>();
            MimeType = mimetype;
            Container = container;
            Type = type;
        }

        /// <summary>
        /// Gets or sets the container.
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
        /// Gets or sets the Dlna OrgPn value.
        /// </summary>
        [XmlAttribute("orgPn")]
        public string? OrgPn { get; set; }

        /// <summary>
        /// Gets or sets the Mime type.
        /// </summary>
        [XmlAttribute("mimeType")]
        public string? MimeType { get; set; }

        /// <summary>
        /// Gets or sets the Conditions.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public ProfileCondition[] Conditions { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Get the containers.
        /// </summary>
        /// <returns>An array of containers.</returns>
        public string[] GetContainers()
        {
            return ContainerProfile.SplitValue(Container);
        }

        /// <summary>
        /// Gets the audio codecs.
        /// </summary>
        /// <returns>An array of audio codecs.</returns>
        public string[] GetAudioCodecs()
        {
            return ContainerProfile.SplitValue(AudioCodec);
        }

        /// <summary>
        /// Gets the Video Codecs.
        /// </summary>
        /// <returns>An array of video codecs.</returns>
        public string[] GetVideoCodecs()
        {
            return ContainerProfile.SplitValue(VideoCodec);
        }
    }
}
