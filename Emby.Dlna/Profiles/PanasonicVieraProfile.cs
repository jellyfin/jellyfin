#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class PanasonicVieraProfile : DefaultProfile
    {
        public PanasonicVieraProfile()
        {
            Name = "Panasonic Viera";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"VIERA",
                Manufacturer = "Panasonic",

                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = "Panasonic MIL DLNA",
                        Match = HeaderMatchType.Substring
                    }
                }
            };

            AddXmlRootAttribute("xmlns:pv", "http://www.pv.com/pvns/");

            TimelineOffsetSeconds = 10;

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
                    AudioCodec = "ac3",
                    VideoCodec = "h264",
                    Type = DlnaProfileType.Video
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
                    Container = "mpeg,mpg",
                    VideoCodec = "mpeg2video,mpeg4",
                    AudioCodec = "ac3,mp3,pcm_dvd",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mkv",
                    VideoCodec = "h264,mpeg2video",
                    AudioCodec = "aac,ac3,dca,mp3,mp2,pcm,dts",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "ts,mpegts",
                    VideoCodec = "h264,mpeg2video",
                    AudioCodec = "aac,mp3,mp2",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mp4,m4v",
                    VideoCodec = "h264",
                    AudioCodec = "aac,ac3,mp3,pcm",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mov",
                    VideoCodec = "h264",
                    AudioCodec = "aac,pcm",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "avi",
                    VideoCodec = "mpeg4",
                    AudioCodec = "pcm",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "flv",
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "mp4",
                    AudioCodec = "aac",
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

                    Conditions = new[]
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
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        }
                    }
                }
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.Embed
                },
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.External
                }
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile
                {
                    Type = DlnaProfileType.Video,
                    Container = "ts,mpegts",
                    OrgPn = "MPEG_TS_SD_EU,MPEG_TS_SD_NA,MPEG_TS_SD_KO",
                    MimeType = "video/vnd.dlna.mpeg-tts"
                },
                new ResponseProfile
                {
                    Container = "m4v",
                    Type = DlnaProfileType.Video,
                    MimeType = "video/mp4"
                }
            };
        }
    }
}
