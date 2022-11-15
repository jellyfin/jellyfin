#pragma warning disable CA1819 // Properties should not return arrays
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Jellyfin.Extensions;
using MediaBrowser.Model.MediaInfo;

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

        /// <summary>
        /// The GetSupportedMediaTypes.
        /// </summary>
        /// <returns>The .</returns>
        public string[] GetSupportedMediaTypes()
        {
            return ContainerProfile.SplitValue(SupportedMediaTypes);
        }

        /// <summary>
        /// Gets the audio transcoding profile.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="audioCodec">The audio Codec.</param>
        /// <returns>A <see cref="TranscodingProfile"/>.</returns>
        public TranscodingProfile? GetAudioTranscodingProfile(string? container, string? audioCodec)
        {
            container = (container ?? string.Empty).TrimStart('.');

            foreach (var i in TranscodingProfiles)
            {
                if (i.Type != DlnaProfileType.Audio)
                {
                    continue;
                }

                if (!string.Equals(container, i.Container, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!i.GetAudioCodecs().Contains(audioCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return i;
            }

            return null;
        }

        /// <summary>
        /// Gets the video transcoding profile.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="audioCodec">The audio Codec.</param>
        /// <param name="videoCodec">The video Codec.</param>
        /// <returns>The <see cref="TranscodingProfile"/>.</returns>
        public TranscodingProfile? GetVideoTranscodingProfile(string? container, string? audioCodec, string? videoCodec)
        {
            container = (container ?? string.Empty).TrimStart('.');

            foreach (var i in TranscodingProfiles)
            {
                if (i.Type != DlnaProfileType.Video)
                {
                    continue;
                }

                if (!string.Equals(container, i.Container, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!i.GetAudioCodecs().Contains(audioCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(videoCodec, i.VideoCodec, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return i;
            }

            return null;
        }

        /// <summary>
        /// Gets the audio media profile.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="audioCodec">The audio codec.</param>
        /// <param name="audioChannels">The audio channels.</param>
        /// <param name="audioBitrate">The audio bitrate.</param>
        /// <param name="audioSampleRate">The audio sample rate.</param>
        /// <param name="audioBitDepth">The audio bit depth.</param>
        /// <returns>The <see cref="ResponseProfile"/>.</returns>
        public ResponseProfile? GetAudioMediaProfile(string container, string? audioCodec, int? audioChannels, int? audioBitrate, int? audioSampleRate, int? audioBitDepth)
        {
            foreach (var i in ResponseProfiles)
            {
                if (i.Type != DlnaProfileType.Audio)
                {
                    continue;
                }

                if (!ContainerProfile.ContainsContainer(i.GetContainers(), container))
                {
                    continue;
                }

                var audioCodecs = i.GetAudioCodecs();
                if (audioCodecs.Length > 0 && !audioCodecs.Contains(audioCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var anyOff = false;
                foreach (ProfileCondition c in i.Conditions)
                {
                    if (!ConditionProcessor.IsAudioConditionSatisfied(GetModelProfileCondition(c), audioChannels, audioBitrate, audioSampleRate, audioBitDepth))
                    {
                        anyOff = true;
                        break;
                    }
                }

                if (anyOff)
                {
                    continue;
                }

                return i;
            }

            return null;
        }

        /// <summary>
        /// Gets the model profile condition.
        /// </summary>
        /// <param name="c">The c<see cref="ProfileCondition"/>.</param>
        /// <returns>The <see cref="ProfileCondition"/>.</returns>
        private ProfileCondition GetModelProfileCondition(ProfileCondition c)
        {
            return new ProfileCondition
            {
                Condition = c.Condition,
                IsRequired = c.IsRequired,
                Property = c.Property,
                Value = c.Value
            };
        }

        /// <summary>
        /// Gets the image media profile.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns>The <see cref="ResponseProfile"/>.</returns>
        public ResponseProfile? GetImageMediaProfile(string container, int? width, int? height)
        {
            foreach (var i in ResponseProfiles)
            {
                if (i.Type != DlnaProfileType.Photo)
                {
                    continue;
                }

                if (!ContainerProfile.ContainsContainer(i.GetContainers(), container))
                {
                    continue;
                }

                var anyOff = false;
                foreach (var c in i.Conditions)
                {
                    if (!ConditionProcessor.IsImageConditionSatisfied(GetModelProfileCondition(c), width, height))
                    {
                        anyOff = true;
                        break;
                    }
                }

                if (anyOff)
                {
                    continue;
                }

                return i;
            }

            return null;
        }

        /// <summary>
        /// Gets the video media profile.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="audioCodec">The audio codec.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="bitDepth">The bit depth.</param>
        /// <param name="videoBitrate">The video bitrate.</param>
        /// <param name="videoProfile">The video profile.</param>
        /// <param name="videoRangeType">The video range type.</param>
        /// <param name="videoLevel">The video level.</param>
        /// <param name="videoFramerate">The video framerate.</param>
        /// <param name="packetLength">The packet length.</param>
        /// <param name="timestamp">The timestamp<see cref="TransportStreamTimestamp"/>.</param>
        /// <param name="isAnamorphic">True if anamorphic.</param>
        /// <param name="isInterlaced">True if interlaced.</param>
        /// <param name="refFrames">The ref frames.</param>
        /// <param name="numVideoStreams">The number of video streams.</param>
        /// <param name="numAudioStreams">The number of audio streams.</param>
        /// <param name="videoCodecTag">The video Codec tag.</param>
        /// <param name="isAvc">True if Avc.</param>
        /// <returns>The <see cref="ResponseProfile"/>.</returns>
        public ResponseProfile? GetVideoMediaProfile(
            string container,
            string? audioCodec,
            string? videoCodec,
            int? width,
            int? height,
            int? bitDepth,
            int? videoBitrate,
            string videoProfile,
            string videoRangeType,
            double? videoLevel,
            float? videoFramerate,
            int? packetLength,
            TransportStreamTimestamp timestamp,
            bool? isAnamorphic,
            bool? isInterlaced,
            int? refFrames,
            int? numVideoStreams,
            int? numAudioStreams,
            string videoCodecTag,
            bool? isAvc)
        {
            foreach (var i in ResponseProfiles)
            {
                if (i.Type != DlnaProfileType.Video)
                {
                    continue;
                }

                if (!ContainerProfile.ContainsContainer(i.GetContainers(), container))
                {
                    continue;
                }

                var audioCodecs = i.GetAudioCodecs();
                if (audioCodecs.Length > 0 && !audioCodecs.Contains(audioCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var videoCodecs = i.GetVideoCodecs();
                if (videoCodecs.Length > 0 && !videoCodecs.Contains(videoCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var anyOff = false;
                foreach (ProfileCondition c in i.Conditions)
                {
                    if (!ConditionProcessor.IsVideoConditionSatisfied(GetModelProfileCondition(c), width, height, bitDepth, videoBitrate, videoProfile, videoRangeType, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                    {
                        anyOff = true;
                        break;
                    }
                }

                if (anyOff)
                {
                    continue;
                }

                return i;
            }

            return null;
        }
    }
}
