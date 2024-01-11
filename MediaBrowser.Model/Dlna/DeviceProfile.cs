#pragma warning disable CA1819 // Properties should not return arrays

using System;
using System.ComponentModel;
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
    [XmlRoot("Profile")]
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
        /// Gets or sets the Identification.
        /// </summary>
        public DeviceIdentification? Identification { get; set; }

        /// <summary>
        /// Gets or sets the friendly name of the device profile, which can be shown to users.
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer of the device which this profile represents.
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets an url for the manufacturer of the device which this profile represents.
        /// </summary>
        public string? ManufacturerUrl { get; set; }

        /// <summary>
        /// Gets or sets the model name of the device which this profile represents.
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// Gets or sets the model description of the device which this profile represents.
        /// </summary>
        public string? ModelDescription { get; set; }

        /// <summary>
        /// Gets or sets the model number of the device which this profile represents.
        /// </summary>
        public string? ModelNumber { get; set; }

        /// <summary>
        /// Gets or sets the ModelUrl.
        /// </summary>
        public string? ModelUrl { get; set; }

        /// <summary>
        /// Gets or sets the serial number of the device which this profile represents.
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableAlbumArtInDidl.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableAlbumArtInDidl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableSingleAlbumArtLimit.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableSingleAlbumArtLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableSingleSubtitleLimit.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableSingleSubtitleLimit { get; set; }

        /// <summary>
        /// Gets or sets the SupportedMediaTypes.
        /// </summary>
        public string SupportedMediaTypes { get; set; } = "Audio,Photo,Video";

        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the AlbumArtPn.
        /// </summary>
        public string? AlbumArtPn { get; set; }

        /// <summary>
        /// Gets or sets the MaxAlbumArtWidth.
        /// </summary>
        public int? MaxAlbumArtWidth { get; set; }

        /// <summary>
        /// Gets or sets the MaxAlbumArtHeight.
        /// </summary>
        public int? MaxAlbumArtHeight { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed width of embedded icons.
        /// </summary>
        public int? MaxIconWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed height of embedded icons.
        /// </summary>
        public int? MaxIconHeight { get; set; }

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
        /// Gets or sets the content of the aggregationFlags element in the urn:schemas-sonycom:av namespace.
        /// </summary>
        public string? SonyAggregationFlags { get; set; }

        /// <summary>
        /// Gets or sets the ProtocolInfo.
        /// </summary>
        public string? ProtocolInfo { get; set; }

        /// <summary>
        /// Gets or sets the TimelineOffsetSeconds.
        /// </summary>
        [DefaultValue(0)]
        public int TimelineOffsetSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether RequiresPlainVideoItems.
        /// </summary>
        [DefaultValue(false)]
        public bool RequiresPlainVideoItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether RequiresPlainFolders.
        /// </summary>
        [DefaultValue(false)]
        public bool RequiresPlainFolders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableMSMediaReceiverRegistrar.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableMSMediaReceiverRegistrar { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IgnoreTranscodeByteRangeRequests.
        /// </summary>
        [DefaultValue(false)]
        public bool IgnoreTranscodeByteRangeRequests { get; set; }

        /// <summary>
        /// Gets or sets the XmlRootAttributes.
        /// </summary>
        public XmlAttribute[] XmlRootAttributes { get; set; } = Array.Empty<XmlAttribute>();

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
        /// Gets or sets the ResponseProfiles.
        /// </summary>
        public ResponseProfile[] ResponseProfiles { get; set; } = Array.Empty<ResponseProfile>();

        /// <summary>
        /// Gets or sets the subtitle profiles.
        /// </summary>
        public SubtitleProfile[] SubtitleProfiles { get; set; } = Array.Empty<SubtitleProfile>();
    }
}
