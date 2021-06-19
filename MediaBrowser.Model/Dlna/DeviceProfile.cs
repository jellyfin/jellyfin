using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MediaBrowser.Model.Extensions;
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
    public class DeviceProfile : DeviceIdentification
    {
        /// <summary>
        /// String constants for the media types.
        /// </summary>
        private const string AllMedia = "Audio,Video,Photo";

        /// <summary>
        /// Contains the parsed representation of <see cref="_supportedMediaTypeString"/>.
        /// </summary>
        private DlnaProfileType[] _supportedMediaTypes = new[]
        {
            DlnaProfileType.Audio,
            DlnaProfileType.Video,
            DlnaProfileType.Photo
        };

        /// <summary>
        /// Contains the string representation of <see cref="_supportedMediaTypes"/>.
        /// </summary>
        private string _supportedMediaTypeString = AllMedia;

        /// <summary>
        /// Gets or sets the identification information that is used to detect this device.
        /// </summary>
        public DeviceIdentification? Identification { get; set; }

        /// <summary>
        /// Gets or sets the name of this device profile. User profiles must have a unique name.
        /// </summary>
        public string Name { get; set; } = "Generic Device";

        /// <summary>
        /// Gets or sets a value indicating whether DIDL should be html encoded or left clear.
        /// </summary>
        [DefaultValue(true)]
        public bool EncodeContextOnTransmission { get; set; } = true;

        /// <summary>
        /// Gets or sets the unique internal identifier.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets a value indicating whether Album art should be included in the Didl response.
        /// <seealso cref=" EnableSingleAlbumArtLimit"/>.
        /// Only works with Audio and Video.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableAlbumArtInDidl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether only one image should be included in the Didl, or if multiple should.
        /// Only applicable with audio and video.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableSingleAlbumArtLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether EnableSingleSubtitleLimit.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableSingleSubtitleLimit { get; set; }

        /// <summary>
        /// Gets or sets the supported media types.
        /// </summary>
        [DefaultValue(AllMedia)]
        public string SupportedMediaTypes
        {
            get => _supportedMediaTypeString;
            set
            {
                _supportedMediaTypeString = value ?? AllMedia;
                _supportedMediaTypes = _supportedMediaTypeString
                    .Split(',')
                    .Select(i => Enum.Parse<DlnaProfileType>(i, true))
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the UserId that this device should use.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the profileID attribute for the AlbumArtURI response.
        /// </summary>
        public string? AlbumArtPn { get; set; }

        /// <summary>
        /// Gets or sets the maximum album art width.
        /// See also <seealso cref="MaxAlbumArtHeight"/>, <see cref="EnableSingleAlbumArtLimit"/>, <seealso cref="EnableAlbumArtInDidl"/>.
        /// </summary>
        public int? MaxAlbumArtWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum album art height.
        /// See also <seealso cref="MaxAlbumArtWidth"/>, <see cref="EnableSingleAlbumArtLimit"/>, <seealso cref="EnableAlbumArtInDidl"/>.
        /// </summary>
        public int? MaxAlbumArtHeight { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed width of embedded icons. See also <seealso cref="MaxIconHeight"/>.
        /// </summary>
        public int? MaxIconWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed height of embedded icons. See also <seealso cref="MaxIconWidth"/>.
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
        /// Gets or sets the ProtocolInfo settings reported by the device.
        /// </summary>
        public string? ProtocolInfo { get; set; }

        /// <summary>
        /// Gets or sets the TimelineOffsetSeconds.
        /// </summary>
        [DefaultValue(0)]
        public int TimelineOffsetSeconds { get; set; } // TODO: Not used anywhere but set in profiles.

        /// <summary>
        /// Gets or sets a value indicating whether all movie media types should be reported as
        /// 'object.item.videoItem', or if appropriate, as 'object.item.videoItem.movie' or 'object.item.videoItem.musicVideoClip'.
        /// </summary>
        [DefaultValue(false)]
        public bool RequiresPlainVideoItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether genre should be reported as 'object.container.storageFolder',
        /// or if appropriate as 'object.container.genre.musicGenre' / 'object.container.genre'.
        /// </summary>
        [DefaultValue(false)]
        public bool RequiresPlainFolders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the MSMediaReceiverRegistrar service should be activated.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableMSMediaReceiverRegistrar { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IgnoreTranscodeByteRangeRequests.
        /// </summary>
        [DefaultValue(false)]
        public bool IgnoreTranscodeByteRangeRequests { get; set; } // TODO: Not used anywhere but set in profiles.

        /// <summary>
        /// Gets or sets the optional root attributes that will be included in the Didl response.
        /// </summary>
        public XmlAttribute[]? XmlRootAttributes { get; set; }

        /// <summary>
        /// Gets or sets the direct play profiles.
        /// </summary>
        public DirectPlayProfile[]? DirectPlayProfiles { get; set; }

        /// <summary>
        /// Gets or sets the transcoding profiles.
        /// </summary>
        public TranscodingProfile[]? TranscodingProfiles { get; set; }

        /// <summary>
        /// Gets or sets the container profiles. Failing to meet these optional conditions causes transcoding to occur.
        /// </summary>
        public ContainerProfile[] ContainerProfiles { get; set; } = Array.Empty<ContainerProfile>();

        /// <summary>
        /// Gets or sets the codec profiles.
        /// </summary>
        public CodecProfile[] CodecProfiles { get; set; } = Array.Empty<CodecProfile>();

        /// <summary>
        /// Gets or sets the response profiles.
        /// </summary>
        public ResponseProfile[]? ResponseProfiles { get; set; }

        /// <summary>
        /// Gets or sets the subtitle profiles.
        /// </summary>
        public SubtitleProfile[] SubtitleProfiles { get; set; } = Array.Empty<SubtitleProfile>();

        /// <summary>
        /// Gets or sets the profile type.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public DeviceProfileType ProfileType { get; set; }

        /// <summary>
        /// Gets or sets the optional profile file path.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public string? Path { get; set; }

        /// <summary>
        /// Verifies that the device supports this type of media.
        /// </summary>
        /// <param name="type">Media type.</param>
        /// <returns>True if the profile supports the media type.</returns>
        public bool IsMediaTypeSupported(string type)
        {
            if (!Enum.TryParse<DlnaProfileType>(type, true, out var mediaType))
            {
                return false;
            }

            return _supportedMediaTypes.Contains(mediaType);
        }

        /// <summary>
        /// Gets the audio transcoding profile.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="audioCodec">The audio Codec.</param>
        /// <returns>A <see cref="TranscodingProfile"/>.</returns>
        public TranscodingProfile? GetAudioTranscodingProfile(string? container, string? audioCodec)
        {
            if (TranscodingProfiles == null)
            {
                return null;
            }

            container = container?.TrimStart('.');

            for (int i = 0; i < TranscodingProfiles.Length; i++)
            {
                var profile = TranscodingProfiles[i];

                if ((profile.Type == DlnaProfileType.Audio)
                    && string.Equals(container, profile.Container, StringComparison.OrdinalIgnoreCase)
                    && (profile.AudioCodec?.ContainsContainer(audioCodec) ?? true))
                {
                    return profile;
                }
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
            if (TranscodingProfiles == null)
            {
                return null;
            }

            container = container?.TrimStart('.');

            for (int i = 0; i < TranscodingProfiles.Length; i++)
            {
                var profile = TranscodingProfiles[i];
                if ((profile.Type == DlnaProfileType.Video)
                    && string.Equals(container, profile.Container, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(videoCodec, profile.VideoCodec, StringComparison.OrdinalIgnoreCase)
                    && (profile.AudioCodec?.ContainsContainer(audioCodec) ?? true))
                {
                    return profile;
                }
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
        public ResponseProfile? GetAudioMediaProfile(
            string container,
            string? audioCodec,
            int? audioChannels,
            int? audioBitrate,
            int? audioSampleRate,
            int? audioBitDepth)
        {
            if (ResponseProfiles == null)
            {
                return null;
            }

            for (int i = 0; i < ResponseProfiles.Length; i++)
            {
                var profile = ResponseProfiles[i];

                if ((profile.Type == DlnaProfileType.Audio)
                    && profile.Container.ContainsContainer(container)
                    && (profile.AudioCodec?.ContainsContainer(audioCodec) ?? true))
                {
                    bool anyOf = false;
                    for (int j = 0; j < profile.Conditions.Length; j++)
                    {
                        if (ConditionProcessor.IsAudioConditionSatisfied(
                            profile.Conditions[j],
                            audioChannels,
                            audioBitrate,
                            audioSampleRate,
                            audioBitDepth))
                        {
                            anyOf = true;
                            break;
                        }
                    }

                    if (anyOf)
                    {
                        continue;
                    }

                    return profile;
                }
            }

            return null;
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
            if (ResponseProfiles == null)
            {
                return null;
            }

            for (int i = 0; i < ResponseProfiles.Length; i++)
            {
                var profile = ResponseProfiles[i];
                if ((profile.Type == DlnaProfileType.Photo) && profile.Container.ContainsContainer(container))
                {
                    var anyOf = false;
                    for (int j = 0; j < profile.Conditions.Length; j++)
                    {
                        if (ConditionProcessor.IsImageConditionSatisfied(profile.Conditions[j], width, height))
                        {
                            anyOf = true;
                            break;
                        }
                    }

                    if (anyOf)
                    {
                        continue;
                    }

                    return profile;
                }
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
        /// <param name="timestamp">The <see cref="TransportStreamTimestamp"/>.</param>
        /// <param name="isAnamorphic">True if anamorphic.</param>
        /// <param name="isInterlaced">True if interlaced.</param>
        /// <param name="refFrames">The ref frames.</param>
        /// <param name="numVideoStreams">The number of video streams.</param>
        /// <param name="numAudioStreams">The number of audio streams.</param>
        /// <param name="videoCodecTag">The video Codec tag.</param>
        /// <param name="isAvc">True if Avc.</param>
        /// <returns>The <see cref="ResponseProfile"/>.</returns>
        public ResponseProfile? GetVideoMediaProfile(
            string? container,
            string? audioCodec,
            string? videoCodec,
            int? width,
            int? height,
            int? bitDepth,
            int? videoBitrate,
            string? videoProfile,
            double? videoLevel,
            float? videoFramerate,
            int? packetLength,
            TransportStreamTimestamp timestamp,
            bool? isAnamorphic,
            bool? isInterlaced,
            int? refFrames,
            int? numVideoStreams,
            int? numAudioStreams,
            string? videoCodecTag,
            bool? isAvc)
        {
            if (ResponseProfiles == null)
            {
                return null;
            }

            for (int i = 0; i < ResponseProfiles.Length; i++)
            {
                var profile = ResponseProfiles[i];
                if ((profile.Type == DlnaProfileType.Video)
                    && profile.Container.ContainsContainer(container)
                    && (profile.AudioCodec?.ContainsContainer(audioCodec) ?? true)
                    && (profile.VideoCodec?.ContainsContainer(videoCodec) ?? true))
                {
                    var anyOf = false;
                    for (int j = 0; j < profile.Conditions.Length; j++)
                    {
                        if (ConditionProcessor.IsVideoConditionSatisfied(
                            profile.Conditions[j],
                            width,
                            height,
                            bitDepth,
                            videoBitrate,
                            videoProfile,
                            videoLevel,
                            videoFramerate,
                            packetLength,
                            timestamp,
                            isAnamorphic,
                            isInterlaced,
                            refFrames,
                            numVideoStreams,
                            numAudioStreams,
                            videoCodecTag,
                            isAvc))
                        {
                            anyOf = true;
                            break;
                        }
                    }

                    if (anyOf)
                    {
                        continue;
                    }

                    return profile;
                }
            }

            return null;
        }
    }
}
