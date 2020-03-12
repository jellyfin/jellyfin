#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class PopcornHourProfile : DefaultProfile
    {
        public PopcornHourProfile()
        {
            Name = "Popcorn Hour";

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
                    Container = "mp4",
                    Type = DlnaProfileType.Video,
                    AudioCodec = "aac",
                    VideoCodec = "h264"
                },

                new TranscodingProfile
                {
                    Container = "jpeg",
                    Type = DlnaProfileType.Photo
                }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mp4,mov,m4v",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "aac"
                },

                new DirectPlayProfile
                {
                    Container = "ts,mpegts",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264",
                    AudioCodec = "aac,ac3,eac3,mp3,mp2,pcm"
                },

                new DirectPlayProfile
                {
                    Container = "asf,wmv",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "wmv3,vc1",
                    AudioCodec = "wmav2,wmapro"
                },

                new DirectPlayProfile
                {
                    Container = "avi",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg4,msmpeg4",
                    AudioCodec = "mp3,ac3,eac3,mp2,pcm"
                },

                new DirectPlayProfile
                {
                    Container = "mkv",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264",
                    AudioCodec = "aac,mp3,ac3,eac3,mp2,pcm"
                },
                new DirectPlayProfile
                {
                    Container = "aac,mp3,flac,ogg,wma,wav",
                    Type = DlnaProfileType.Audio
                },
                new DirectPlayProfile
                {
                    Container = "jpeg,gif,bmp,png",
                    Type = DlnaProfileType.Photo
                }
            };

            CodecProfiles = new[]
            {
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec="h264",
                    Conditions = new []
                    {
                        new ProfileCondition(ProfileConditionType.EqualsAny, ProfileConditionValue.VideoProfile, "baseline|constrained baseline"),
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
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.IsAnamorphic,
                            Value = "true",
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
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.IsAnamorphic,
                            Value = "true",
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
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.Audio,
                    Codec = "aac",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioChannels,
                            Value = "2",
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.Audio,
                    Codec = "mp3",
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
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioBitrate,
                            Value = "320000",
                            IsRequired = false
                        }
                    }
                }
            };

            ResponseProfiles = new ResponseProfile[]
            {
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
