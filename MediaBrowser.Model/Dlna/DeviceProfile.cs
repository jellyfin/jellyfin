#pragma warning disable CA1819 // Properties should not return arrays

using System;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// A <see cref="DeviceProfile" /> represents a set of metadata which determines which content a certain device is able to play.
    /// <br/>
    /// Specifically, it defines the supported <see cref="ContainerProfiles">containers</see> and
    /// <see cref="CodecProfiles">codecs</see> (video and/or audio, including codec profiles and levels)
    /// the device is able to direct play (without transcoding or remuxing),
    /// as well as which <see cref="TranscodingProfiles">containers/codecs to transcode to</see> in case it isn't.
    /// </summary>
    public class DeviceProfile
    {
        /// <summary>
        /// Gets or sets the name of this device profile.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [XmlIgnore]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed bitrate for all streamed content.
        /// </summary>
        public int? MaxStreamingBitrate { get; set; } = 8000000;

        /// <summary>
        /// Gets or sets the maximum allowed bitrate for statically streamed content (= direct played files).
        /// </summary>
        public int? MaxStaticBitrate { get; set; } = 8000000;

        /// <summary>
        /// Gets or sets the maximum allowed bitrate for transcoded music streams.
        /// </summary>
        public int? MusicStreamingTranscodingBitrate { get; set; } = 128000;

        /// <summary>
        /// Gets or sets the maximum allowed bitrate for statically streamed (= direct played) music files.
        /// </summary>
        public int? MaxStaticMusicBitrate { get; set; } = 8000000;

        /// <summary>
        /// Gets or sets the direct play profiles.
        /// </summary>
        public DirectPlayProfile[] DirectPlayProfiles { get; set; } = Array.Empty<DirectPlayProfile>();

        /// <summary>
        /// Gets or sets the transcoding profiles.
        /// </summary>
        public TranscodingProfile[] TranscodingProfiles { get; set; } = Array.Empty<TranscodingProfile>();

        /// <summary>
        /// Gets or sets the container profiles.
        /// </summary>
        public ContainerProfile[] ContainerProfiles { get; set; } = Array.Empty<ContainerProfile>();

        /// <summary>
        /// Gets or sets the codec profiles.
        /// </summary>
        public CodecProfile[] CodecProfiles { get; set; } = Array.Empty<CodecProfile>();

        /// <summary>
        /// Gets or sets the subtitle profiles.
        /// </summary>
        public SubtitleProfile[] SubtitleProfiles { get; set; } = Array.Empty<SubtitleProfile>();
    }
}
