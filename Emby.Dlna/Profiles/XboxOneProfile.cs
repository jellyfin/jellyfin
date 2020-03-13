#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class XboxOneProfile : DefaultProfile
    {
        public XboxOneProfile()
        {
            Name = "Xbox One";

            TimelineOffsetSeconds = 40;

            Identification = new DeviceIdentification
            {
                ModelName = "Xbox One",

                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "FriendlyName.DLNA.ORG", Value = "XboxOne", Match = HeaderMatchType.Substring
                    },
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent", Value = "NSPlayer/12", Match = HeaderMatchType.Substring
                    }
                }
            };

            var videoProfile = "high|main|baseline|constrained baseline";
            var videoLevel = "41";

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
                    Container = "jpeg",
                    VideoCodec = "jpeg",
                    Type = DlnaProfileType.Photo
                },
                new TranscodingProfile
                {
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Video
                }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "ts,mpegts",
                    VideoCodec = "h264,mpeg2video,hevc",
                    AudioCodec = "ac3,aac,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "avi",
                    VideoCodec = "mpeg4",
                    AudioCodec = "ac3,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "avi",
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp4,mov,mkv,m4v",
                    VideoCodec = "h264,mpeg4,mpeg2video,hevc",
                    AudioCodec = "aac,ac3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "asf",
                    VideoCodec = "wmv2,wmv3,vc1",
                    AudioCodec = "wmav2,wmapro",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "asf",
                    AudioCodec = "wmav2,wmapro,wmavoice",
                    Type = DlnaProfileType.Audio
                },
                new DirectPlayProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio
                },
                new DirectPlayProfile
                {
                    Container = "jpeg",
                    Type = DlnaProfileType.Photo
                }
            };

            ContainerProfiles = new[]
            {
                new ContainerProfile
                {
                    Type = DlnaProfileType.Video,
                    Container = "mp4,mov",

                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.Has64BitOffsets,
                            Value = "false",
                            IsRequired = false
                        }
                    }
                }
            };

            CodecProfiles = new[]
            {
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec = "mpeg4",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.IsAnamorphic,
                            Value = "true",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Width,
                            Value = "1920"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoFramerate,
                            Value = "30",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitrate,
                            Value = "5120000",
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec = "h264",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.IsAnamorphic,
                            Value = "true",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Width,
                            Value = "1920"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoLevel,
                            Value = videoLevel,
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.EqualsAny,
                            Property = ProfileConditionValue.VideoProfile,
                            Value = videoProfile,
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec = "wmv2,wmv3,vc1",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.IsAnamorphic,
                            Value = "true",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Width,
                            Value = "1920"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoFramerate,
                            Value = "30",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitrate,
                            Value = "15360000",
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.Video,
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.IsAnamorphic,
                            Value = "true",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.VideoAudio,
                    Codec = "ac3,wmav2,wmapro",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioChannels,
                            Value = "6",
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.VideoAudio,
                    Codec = "aac",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioChannels,
                            Value = "2",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.AudioProfile,
                            Value = "lc",
                            IsRequired = false
                        }
                    }
                }
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile
                {
                    Container = "avi",
                    MimeType = "video/avi",
                    Type = DlnaProfileType.Video
                },
                new ResponseProfile
                {
                    Container = "m4v",
                    Type = DlnaProfileType.Video,
                    MimeType = "video/mp4"
                }
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.Embed
                }
            };
        }
    }
}
