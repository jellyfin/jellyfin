using MediaBrowser.Model.Dlna;
using System.Xml.Serialization;

namespace MediaBrowser.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class SonyPs3Profile : DefaultProfile
    {
        public SonyPs3Profile()
        {
            Name = "Sony PlayStation 3";

            Identification = new DeviceIdentification
            {
                FriendlyName = "PLAYSTATION 3",

                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = @"PLAYSTATION 3",
                        Match = HeaderMatchType.Substring
                    },

                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Client-Info",
                        Value = @"PLAYSTATION 3",
                        Match = HeaderMatchType.Substring
                    }
                }
            };

            SonyAggregationFlags = "10";
            XDlnaDoc = "DMS-1.50";

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
                            Value = "2"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.AudioProfile,
                            Value = "he-aac"
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
                }
            };
        }
    }
}
