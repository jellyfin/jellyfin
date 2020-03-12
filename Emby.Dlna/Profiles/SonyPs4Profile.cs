#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SonyPs4Profile : DefaultProfile
    {
        public SonyPs4Profile()
        {
            Name = "Sony PlayStation 4";

            Identification = new DeviceIdentification
            {
                FriendlyName = "PLAYSTATION 4",

                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = @"PLAYSTATION 4",
                        Match = HeaderMatchType.Substring
                    },

                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Client-Info",
                        Value = @"PLAYSTATION 4",
                        Match = HeaderMatchType.Substring
                    }
                }
            };

            AlbumArtPn = "JPEG_TN";

            SonyAggregationFlags = "10";
            EnableSingleAlbumArtLimit = true;

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "avi",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg4",
                    AudioCodec = "mp2,mp3"
                },
                new DirectPlayProfile
                {
                    Container = "ts,mpegts",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video,h264",
                    AudioCodec = "ac3,mp2,mp3,aac"
                },
                new DirectPlayProfile
                {
                    Container = "mpeg",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video",
                    AudioCodec = "mp2"
                },
                new DirectPlayProfile
                {
                    Container = "mp4,mkv,m4v",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "aac,ac3"
                },
                new DirectPlayProfile
                {
                    Container = "aac,mp3,wav",
                    Type = DlnaProfileType.Audio
                },
                new DirectPlayProfile
                {
                    Container = "jpeg,png,gif,bmp,tiff",
                    Type = DlnaProfileType.Photo
                }
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio,
                    // Transcoded audio won't be playable at all without this
                    TranscodeSeekInfo = TranscodeSeekInfo.Bytes
                },
                new TranscodingProfile
                {
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Video
                },
                new TranscodingProfile
                {
                    Container = "jpeg",
                    Type = DlnaProfileType.Photo
                }
            };

            ContainerProfiles = new[]
            {
                new ContainerProfile
                {
                    Type = DlnaProfileType.Photo,

                    Conditions = new []
                    {
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
                        }
                    }
                }
            };

            CodecProfiles = new[]
            {
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec = "h264",

                    Conditions = new []
                    {
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
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoLevel,
                            Value = "41",
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.VideoAudio,
                    Codec = "ac3",

                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioChannels,
                            Value = "6",
                            IsRequired = false
                        },

                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioBitrate,
                            Value = "640000",
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.VideoAudio,
                    Codec = "wmapro",

                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioChannels,
                            Value = "2"
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
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.AudioProfile,
                            Value = "he-aac",
                            IsRequired = false
                        }
                    }
                }
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile
                {
                    Container = "mp4,mov",
                    AudioCodec="aac",
                    MimeType = "video/mp4",
                    Type = DlnaProfileType.Video
                },

                new ResponseProfile
                {
                    Container = "avi",
                    MimeType = "video/divx",
                    OrgPn="AVI",
                    Type = DlnaProfileType.Video
                },

                new ResponseProfile
                {
                    Container = "wav",
                    MimeType = "audio/wav",
                    Type = DlnaProfileType.Audio
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
