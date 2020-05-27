#nullable disable
#pragma warning disable CS1591

using System;
using System.Linq;
using System.Xml.Serialization;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Dlna
{
    [XmlRoot("Profile")]
    public class DeviceProfile
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        [XmlIgnore]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the identification.
        /// </summary>
        /// <value>The identification.</value>
        public DeviceIdentification Identification { get; set; }

        public string FriendlyName { get; set; }
        public string Manufacturer { get; set; }
        public string ManufacturerUrl { get; set; }
        public string ModelName { get; set; }
        public string ModelDescription { get; set; }
        public string ModelNumber { get; set; }
        public string ModelUrl { get; set; }
        public string SerialNumber { get; set; }

        public bool EnableAlbumArtInDidl { get; set; }
        public bool EnableSingleAlbumArtLimit { get; set; }
        public bool EnableSingleSubtitleLimit { get; set; }

        public string SupportedMediaTypes { get; set; }

        public string UserId { get; set; }

        public string AlbumArtPn { get; set; }

        public int MaxAlbumArtWidth { get; set; }
        public int MaxAlbumArtHeight { get; set; }

        public int? MaxIconWidth { get; set; }
        public int? MaxIconHeight { get; set; }

        public long? MaxStreamingBitrate { get; set; }
        public long? MaxStaticBitrate { get; set; }

        public int? MusicStreamingTranscodingBitrate { get; set; }
        public int? MaxStaticMusicBitrate { get; set; }

        /// <summary>
        /// Controls the content of the aggregationFlags element in the urn:schemas-sonycom:av namespace.
        /// </summary>
        public string SonyAggregationFlags { get; set; }

        public string ProtocolInfo { get; set; }

        public int TimelineOffsetSeconds { get; set; }
        public bool RequiresPlainVideoItems { get; set; }
        public bool RequiresPlainFolders { get; set; }

        public bool EnableMSMediaReceiverRegistrar { get; set; }
        public bool IgnoreTranscodeByteRangeRequests { get; set; }

        public XmlAttribute[] XmlRootAttributes { get; set; }

        /// <summary>
        /// Gets or sets the direct play profiles.
        /// </summary>
        /// <value>The direct play profiles.</value>
        public DirectPlayProfile[] DirectPlayProfiles { get; set; }

        /// <summary>
        /// Gets or sets the transcoding profiles.
        /// </summary>
        /// <value>The transcoding profiles.</value>
        public TranscodingProfile[] TranscodingProfiles { get; set; }

        public ContainerProfile[] ContainerProfiles { get; set; }

        public CodecProfile[] CodecProfiles { get; set; }
        public ResponseProfile[] ResponseProfiles { get; set; }

        public SubtitleProfile[] SubtitleProfiles { get; set; }

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

        public string[] GetSupportedMediaTypes()
        {
            return ContainerProfile.SplitValue(SupportedMediaTypes);
        }

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

        public ResponseProfile GetVideoMediaProfile(string container,
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
