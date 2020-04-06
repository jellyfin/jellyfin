#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class WdtvLiveProfile : DefaultProfile
    {
        public WdtvLiveProfile()
        {
            Name = "WDTV Live";

            TimelineOffsetSeconds = 5;
            IgnoreTranscodeByteRangeRequests = true;

            Identification = new DeviceIdentification
            {
                ModelName = "WD TV",

                Headers = new[]
                {
                    new HttpHeaderInfo {Name = "User-Agent", Value = "alphanetworks", Match = HeaderMatchType.Substring},
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = "ALPHA Networks",
                        Match = HeaderMatchType.Substring
                    }
                }
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    Type = DlnaProfileType.Audio,
                    AudioCodec = "mp3"
                },
                new TranscodingProfile
                {
                    Container = "ts",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264",
                    AudioCodec = "aac"
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
                    Container = "avi",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video,mpeg4,h264,vc1",
                    AudioCodec = "ac3,eac3,dca,mp2,mp3,pcm,dts"
                },

                new DirectPlayProfile
                {
                    Container = "mpeg",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video",
                    AudioCodec = "ac3,eac3,dca,mp2,mp3,pcm,dts"
                },

                new DirectPlayProfile
                {
                    Container = "mkv",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video,mpeg4,h264,vc1",
                    AudioCodec = "ac3,eac3,dca,aac,mp2,mp3,pcm,dts"
                },

                new DirectPlayProfile
                {
                    Container = "ts,m2ts,mpegts",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video,h264,vc1",
                    AudioCodec = "ac3,eac3,dca,mp2,mp3,aac,dts"
                },

                new DirectPlayProfile
                {
                    Container = "mp4,mov,m4v",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "ac3,eac3,aac,mp2,mp3,dca,dts"
                },

                new DirectPlayProfile
                {
                    Container = "asf",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "vc1",
                    AudioCodec = "wmav2,wmapro"
                },

                new DirectPlayProfile
                {
                    Container = "asf",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg2video",
                    AudioCodec = "mp2,ac3"
                },

                new DirectPlayProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp2,mp3",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "mp4",
                    AudioCodec = "mp4",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "flac",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "asf",
                    AudioCodec = "wmav2,wmapro,wmavoice",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "ogg",
                    AudioCodec = "vorbis",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Type = DlnaProfileType.Photo,

                    Container = "jpeg,png,gif,bmp,tiff"
                }
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile
                {
                    Container = "ts,mpegts",
                    OrgPn = "MPEG_TS_SD_NA",
                    Type = DlnaProfileType.Video
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
                            Property = ProfileConditionValue.VideoLevel,
                            Value = "41"
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
                        }
                    }
                }
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.External
                },
                new SubtitleProfile
                {
                    Format = "srt",
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
                    Format = "idx",
                    Method = SubtitleDeliveryMethod.Embed
                }
            };
        }
    }
}
