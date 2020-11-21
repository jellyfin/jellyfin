#nullable disable
#pragma warning disable CA1819 // Properties should not return arrays
using System;
using System.Linq;
using System.Xml.Serialization;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DeviceProfile" />.
    /// </summary>
    [XmlRoot("Profile")]
    public class DeviceProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProfile"/> class.
        /// </summary>
        public DeviceProfile()
        {
            DirectPlayProfiles = Array.Empty<DirectPlayProfile>();
            TranscodingProfiles = Array.Empty<TranscodingProfile>();
            ResponseProfiles = Array.Empty<ResponseProfile>();
            CodecProfiles = Array.Empty<CodecProfile>();
            ContainerProfiles = Array.Empty<ContainerProfile>();
            SubtitleProfiles = Array.Empty<SubtitleProfile>();

            XmlRootAttributes = Array.Empty<XmlAttribute>();

            SupportedMediaTypes = "Audio,Photo,Video";
            MaxStreamingBitrate = 8000000;
            MaxStaticBitrate = 8000000;
            MusicStreamingTranscodingBitrate = 128000;
        }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [XmlIgnore]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Identification.
        /// </summary>
        public DeviceIdentification Identification { get; set; }

        /// <summary>
        /// Gets or sets the FriendlyName.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the Manufacturer.
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the ManufacturerUrl.
        /// </summary>
        public string ManufacturerUrl { get; set; }

        /// <summary>
        /// Gets or sets the ModelName.
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets the ModelDescription.
        /// </summary>
        public string ModelDescription { get; set; }

        /// <summary>
        /// Gets or sets the ModelNumber.
        /// </summary>
        public string ModelNumber { get; set; }

        /// <summary>
        /// Gets or sets the ModelUrl.
        /// </summary>
        public string ModelUrl { get; set; }

        /// <summary>
        /// Gets or sets the SerialNumber.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableAlbumArtInDidl.
        /// </summary>
        public bool EnableAlbumArtInDidl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableSingleAlbumArtLimit.
        /// </summary>
        public bool EnableSingleAlbumArtLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableSingleSubtitleLimit.
        /// </summary>
        public bool EnableSingleSubtitleLimit { get; set; }

        /// <summary>
        /// Gets or sets the SupportedMediaTypes.
        /// </summary>
        public string SupportedMediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the AlbumArtPn.
        /// </summary>
        public string AlbumArtPn { get; set; }

        /// <summary>
        /// Gets or sets the MaxAlbumArtWidth.
        /// </summary>
        public int MaxAlbumArtWidth { get; set; }

        /// <summary>
        /// Gets or sets the MaxAlbumArtHeight.
        /// </summary>
        public int MaxAlbumArtHeight { get; set; }

        /// <summary>
        /// Gets or sets the MaxIconWidth.
        /// </summary>
        public int? MaxIconWidth { get; set; }

        /// <summary>
        /// Gets or sets the MaxIconHeight.
        /// </summary>
        public int? MaxIconHeight { get; set; }

        /// <summary>
        /// Gets or sets the MaxStreamingBitrate.
        /// </summary>
        public int? MaxStreamingBitrate { get; set; }

        /// <summary>
        /// Gets or sets the MaxStaticBitrate.
        /// </summary>
        public int? MaxStaticBitrate { get; set; }

        /// <summary>
        /// Gets or sets the MusicStreamingTranscodingBitrate.
        /// </summary>
        public int? MusicStreamingTranscodingBitrate { get; set; }

        /// <summary>
        /// Gets or sets the MaxStaticMusicBitrate.
        /// </summary>
        public int? MaxStaticMusicBitrate { get; set; }

        /// <summary>
        /// Gets or sets the content of the aggregationFlags element in the urn:schemas-sonycom:av namespace.
        /// </summary>
        public string SonyAggregationFlags { get; set; }

        /// <summary>
        /// Gets or sets the ProtocolInfo.
        /// </summary>
        public string ProtocolInfo { get; set; }

        /// <summary>
        /// Gets or sets the TimelineOffsetSeconds.
        /// </summary>
        public int TimelineOffsetSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether RequiresPlainVideoItems.
        /// </summary>
        public bool RequiresPlainVideoItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether RequiresPlainFolders.
        /// </summary>
        public bool RequiresPlainFolders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableMSMediaReceiverRegistrar.
        /// </summary>
        public bool EnableMSMediaReceiverRegistrar { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IgnoreTranscodeByteRangeRequests.
        /// </summary>
        public bool IgnoreTranscodeByteRangeRequests { get; set; }

        /// <summary>
        /// Gets or sets the XmlRootAttributes.
        /// </summary>
        public XmlAttribute[] XmlRootAttributes { get; set; }

        /// <summary>
        /// Gets or sets the direct play profiles.
        /// </summary>
        public DirectPlayProfile[] DirectPlayProfiles { get; set; }

        /// <summary>
        /// Gets or sets the transcoding profiles.
        /// </summary>
        public TranscodingProfile[] TranscodingProfiles { get; set; }

        /// <summary>
        /// Gets or sets the ContainerProfiles.
        /// </summary>
        public ContainerProfile[] ContainerProfiles { get; set; }

        /// <summary>
        /// Gets or sets the CodecProfiles.
        /// </summary>
        public CodecProfile[] CodecProfiles { get; set; }

        /// <summary>
        /// Gets or sets the ResponseProfiles.
        /// </summary>
        public ResponseProfile[] ResponseProfiles { get; set; }

        /// <summary>
        /// Gets or sets the SubtitleProfiles.
        /// </summary>
        public SubtitleProfile[] SubtitleProfiles { get; set; }

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
        public TranscodingProfile GetAudioTranscodingProfile(string container, string audioCodec)
        {
            container = (container ?? string.Empty).TrimStart('.');

            foreach (var i in TranscodingProfiles)
            {
                if (i.Type != MediaBrowser.Model.Dlna.DlnaProfileType.Audio)
                {
                    continue;
                }

                if (!string.Equals(container, i.Container, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!i.GetAudioCodecs().Contains(audioCodec ?? string.Empty, StringComparer.OrdinalIgnoreCase))
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
        public TranscodingProfile GetVideoTranscodingProfile(string container, string audioCodec, string videoCodec)
        {
            container = (container ?? string.Empty).TrimStart('.');

            foreach (var i in TranscodingProfiles)
            {
                if (i.Type != MediaBrowser.Model.Dlna.DlnaProfileType.Video)
                {
                    continue;
                }

                if (!string.Equals(container, i.Container, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!i.GetAudioCodecs().Contains(audioCodec ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(videoCodec, i.VideoCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
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
        public ResponseProfile GetAudioMediaProfile(string container, string audioCodec, int? audioChannels, int? audioBitrate, int? audioSampleRate, int? audioBitDepth)
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
                if (audioCodecs.Length > 0 && !audioCodecs.Contains(audioCodec ?? string.Empty, StringComparer.OrdinalIgnoreCase))
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
        public ResponseProfile GetImageMediaProfile(string container, int? width, int? height)
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
        public ResponseProfile GetVideoMediaProfile(
            string container,
            string audioCodec,
            string videoCodec,
            int? width,
            int? height,
            int? bitDepth,
            int? videoBitrate,
            string videoProfile,
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
                if (audioCodecs.Length > 0 && !audioCodecs.Contains(audioCodec ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var videoCodecs = i.GetVideoCodecs();
                if (videoCodecs.Length > 0 && !videoCodecs.Contains(videoCodec ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var anyOff = false;
                foreach (ProfileCondition c in i.Conditions)
                {
                    if (!ConditionProcessor.IsVideoConditionSatisfied(GetModelProfileCondition(c), width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
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
