using System;
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
    ///
    /// Specifically, it defines the supported <see cref="ContainerProfiles">containers</see> and <see cref="CodecProfiles">codecs</see>
    /// (video and/or audio, including codec profiles and levels) the device is able to direct play (without transcoding or remuxing),
    /// as well as which <see cref="TranscodingProfiles">containers/codecs to transcode to</see> in case it isn't.
    /// </summary>
    [XmlRoot("Profile")]
    public class DeviceProfile
    {
        /// <summary>
        /// String constants for the media types.
        /// </summary>
        private const string AllMedia = "Audio,Video,Photo";

        /// <summary>
        /// Holds the default profile lazy created by <see cref="DefaultProfile"/>.
        /// </summary>
        private static DeviceProfile? _defaultProfile;

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
        /// Initializes a new instance of the <see cref="DeviceProfile"/> class.
        /// </summary>
        public DeviceProfile()
        {
            // Required for serialization.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProfile"/> class.
        /// </summary>
        /// <param name="source">Optional <see cref="DeviceProfile"/> from which to copy the settings.</param>
        /// <param name="name">Optional. Profile name.</param>
        public DeviceProfile(DeviceProfile source, string? name = null)
        {
            CopyFrom(source);
            Name = name ?? source.Name;
        }

        /// <summary>
        /// Gets or sets the identification information that is used to match to this device.
        /// </summary>
        public DeviceIdentification? Identification { get; set; }

        /// <summary>
        /// Gets or sets the ip address.
        /// </summary>
        public string? Address { get; set; }

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
        /// Gets or sets the supported media types as a string.
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
        /// Initializes a new instance of the default <see cref="DeviceProfile"/> class.
        /// </summary>
        /// <returns>A <see cref="DeviceProfile"/> set to the default profile.</returns>
        public static DeviceProfile DefaultProfile()
        {
            if (_defaultProfile == null)
            {
                _defaultProfile = new DeviceProfile()
                {
                    AlbumArtPn = "JPEG_SM",
                    EnableAlbumArtInDidl = false,
                    EncodeContextOnTransmission = true,
                    Id = Guid.Empty,
                    MaxAlbumArtHeight = 480,
                    MaxAlbumArtWidth = 480,
                    MaxIconHeight = 48,
                    MaxIconWidth = 48,
                    MaxStreamingBitrate = 140000000,
                    MusicStreamingTranscodingBitrate = 192000,
                    Name = "Default Profile",
                    ProfileType = DeviceProfileType.SystemTemplate,
                    ProtocolInfo = "http-get:*:video/mpeg:*,http-get:*:video/mp4:*,http-get:*:video/vnd.dlna.mpeg-tts:*,http-get:*:video/avi:*,http-get:*:video/x-matroska:*,http-get:*:video/x-ms-wmv:*,http-get:*:video/wtv:*,http-get:*:audio/mpeg:*,http-get:*:audio/mp3:*,http-get:*:audio/mp4:*,http-get:*:audio/x-ms-wma:*,http-get:*:audio/wav:*,http-get:*:audio/L16:*,http-get:*:image/jpeg:*,http-get:*:image/png:*,http-get:*:image/gif:*,http-get:*:image/tiff:*",
                    TranscodingProfiles = new[]
                    {
                        new TranscodingProfile
                        {
                            Container = "mp3",
                            AudioCodec = "mp3",
                            Type = DlnaProfileType.Audio
                        },
                        new TranscodingProfile
                        {
                            Container = "ts",
                            Type = DlnaProfileType.Video,
                            AudioCodec = "aac",
                            VideoCodec = "h264"
                        },
                        new TranscodingProfile
                        {
                            Container = "jpeg",
                            Type = DlnaProfileType.Photo
                        }
                    },
                    DirectPlayProfiles = new[]
                    {
                        new DirectPlayProfile
                        {
                            // play all
                            Container = string.Empty,
                            Type = DlnaProfileType.Video
                        },

                        new DirectPlayProfile
                        {
                            // play all
                            Container = string.Empty,
                            Type = DlnaProfileType.Audio
                        }
                    },
                    SubtitleProfiles = new[]
                    {
                        new SubtitleProfile
                        {
                            Format = "srt",
                            Method = SubtitleDeliveryMethod.External
                        },

                        new SubtitleProfile
                        {
                            Format = "sub",
                            Method = SubtitleDeliveryMethod.External
                        },
                        new SubtitleProfile
                        {
                            Format = "srt",
                            Method = SubtitleDeliveryMethod.Embed
                        },
                        new SubtitleProfile
                        {
                            Format = "ass",
                            Method = SubtitleDeliveryMethod.Embed
                        },
                        new SubtitleProfile
                        {
                            Format = "ssa",
                            Method = SubtitleDeliveryMethod.Embed
                        },

                        new SubtitleProfile
                        {
                            Format = "smi",
                            Method = SubtitleDeliveryMethod.Embed
                        },
                        new SubtitleProfile
                        {
                            Format = "dvdsub",
                            Method = SubtitleDeliveryMethod.Embed
                        },

                        new SubtitleProfile
                        {
                            Format = "pgs",
                            Method = SubtitleDeliveryMethod.Embed
                        },

                        new SubtitleProfile
                        {
                            Format = "pgssub",
                            Method = SubtitleDeliveryMethod.Embed
                        },

                        new SubtitleProfile
                        {
                            Format = "sub",
                            Method = SubtitleDeliveryMethod.Embed
                        },
                        new SubtitleProfile
                        {
                            Format = "subrip",
                            Method = SubtitleDeliveryMethod.Embed
                        },
                        new SubtitleProfile
                        {
                            Format = "vtt",
                            Method = SubtitleDeliveryMethod.Embed
                        }
                    },
                    ResponseProfiles = new[]
                    {
                        new ResponseProfile
                        {
                            Container = "m4v",
                            Type = DlnaProfileType.Video,
                            MimeType = "video/mp4"
                        }
                    }
                };
            }

            return _defaultProfile;
        }

        /// <summary>
        /// Verifies that the device supports this type of media.
        /// </summary>
        /// <param name="type">Media type.</param>
        /// <returns>True if the profile supports the media type.</returns>
        public bool IsMediaTypeSupported(string type)
        {
            if (Enum.TryParse<DlnaProfileType>(type, true, out var mediaType))
            {
                return _supportedMediaTypes.Contains(mediaType);
            }

            return false;
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

        /// <summary>
        /// Copies the values from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The <see cref="DeviceProfile"/> to copy.</param>
        public void CopyFrom(DeviceProfile source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Address = source.Address;
            AlbumArtPn = source.AlbumArtPn;
            CodecProfiles = source.CodecProfiles;
            ContainerProfiles = source.ContainerProfiles;
            DirectPlayProfiles = source.DirectPlayProfiles;
            EnableAlbumArtInDidl = source.EnableAlbumArtInDidl;
            EnableMSMediaReceiverRegistrar = source.EnableMSMediaReceiverRegistrar;
            EnableSingleAlbumArtLimit = source.EnableSingleAlbumArtLimit;
            EnableSingleSubtitleLimit = source.EnableSingleSubtitleLimit;
            EncodeContextOnTransmission = source.EncodeContextOnTransmission;
            IgnoreTranscodeByteRangeRequests = source.IgnoreTranscodeByteRangeRequests;
            MaxAlbumArtHeight = source.MaxAlbumArtHeight;
            MaxAlbumArtWidth = source.MaxAlbumArtWidth;
            MaxIconHeight = source.MaxIconHeight;
            MaxIconWidth = source.MaxIconWidth;
            MaxStaticBitrate = source.MaxStaticBitrate;
            MaxStaticMusicBitrate = source.MaxStaticMusicBitrate;
            MaxStreamingBitrate = source.MaxStreamingBitrate;
            MusicStreamingTranscodingBitrate = source.MusicStreamingTranscodingBitrate;
            ProtocolInfo = source.ProtocolInfo;
            RequiresPlainFolders = source.RequiresPlainFolders;
            RequiresPlainVideoItems = source.RequiresPlainVideoItems;
            ResponseProfiles = source.ResponseProfiles;
            SonyAggregationFlags = source.SonyAggregationFlags;
            SubtitleProfiles = source.SubtitleProfiles;
            _supportedMediaTypes = source._supportedMediaTypes;
            TimelineOffsetSeconds = source.TimelineOffsetSeconds;
            TranscodingProfiles = source.TranscodingProfiles;
            UserId = source.UserId;
            XmlRootAttributes = source.XmlRootAttributes;

            // Path is reset to null, meaning the uniqueness of the name will be checked if saved to disk.
            Path = null;
            ProfileType = DeviceProfileType.Profile;
        }
    }
}
