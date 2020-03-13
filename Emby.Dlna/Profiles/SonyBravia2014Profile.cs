#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SonyBravia2014Profile : DefaultProfile
    {
        public SonyBravia2014Profile()
        {
            Name = "Sony Bravia (2014)";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"(KDL-\d{2}W[5-9]\d{2}B|KDL-\d{2}R480|XBR-\d{2}X[89]\d{2}B|KD-\d{2}[SX][89]\d{3}B).*",
                Manufacturer = "Sony",

                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Client-Info",
                        Value = @".*(KDL-\d{2}W[5-9]\d{2}B|KDL-\d{2}R480|XBR-\d{2}X[89]\d{2}B|KD-\d{2}[SX][89]\d{3}B).*",
                        Match = HeaderMatchType.Regex
                    }
                }
            };

            AddXmlRootAttribute("xmlns:av", "urn:schemas-sony-com:av");

            AlbumArtPn = "JPEG_TN";

            ModelName = "Windows Media Player Sharing";
            ModelNumber = "3.0";
            ModelUrl = "http://www.microsoft.com/";
            Manufacturer = "Microsoft Corporation";
            ManufacturerUrl = "http://www.microsoft.com/";
            SonyAggregationFlags = "10";
            EnableSingleAlbumArtLimit = true;
            EnableAlbumArtInDidl = true;

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    Type = DlnaProfileType.Audio
                },
                new TranscodingProfile
                {
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "ac3",
                    Type = DlnaProfileType.Video,
                    EnableMpegtsM2TsMode = true
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
                    Container = "ts,mpegts",
                    VideoCodec = "h264",
                    AudioCodec = "ac3,eac3,aac,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "ts,mpegts",
                    VideoCodec = "mpeg2video",
                    AudioCodec = "mp3,mp2",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp4,m4v",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "ac3,eac3,aac,mp3,mp2",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mov",
                    VideoCodec = "h264,mpeg4,mjpeg",
                    AudioCodec = "ac3,eac3,aac,mp3,mp2",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mkv",
                    VideoCodec = "h264,mpeg4,vp8",
                    AudioCodec = "ac3,eac3,aac,mp3,mp2,pcm,vorbis",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "avi",
                    VideoCodec = "mpeg4",
                    AudioCodec = "ac3,eac3,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "avi",
                    VideoCodec = "mjpeg",
                    AudioCodec = "pcm",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mpeg",
                    VideoCodec = "mpeg2video,mpeg1video",
                    AudioCodec = "mp3,mp2",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "asf",
                    VideoCodec = "wmv2,wmv3,vc1",
                    AudioCodec = "wmav2,wmapro,wmavoice",
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
                    Container = "wav",
                    AudioCodec = "pcm",
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

            ResponseProfiles = new[]
            {
                new ResponseProfile
                {
                    Container = "ts,mpegts",
                    VideoCodec="h264",
                    AudioCodec="ac3,aac,mp3",
                    MimeType = "video/vnd.dlna.mpeg-tts",
                    OrgPn="AVC_TS_HD_24_AC3_T,AVC_TS_HD_50_AC3_T,AVC_TS_HD_60_AC3_T,AVC_TS_HD_EU_T",
                    Type = DlnaProfileType.Video,

                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.PacketLength,
                            Value = "192"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.VideoTimestamp,
                            Value = "Valid"
                        }
                    }
                },

                new ResponseProfile
                {
                    Container = "ts,mpegts",
                    VideoCodec="h264",
                    AudioCodec="ac3,aac,mp3",
                    MimeType = "video/mpeg",
                    OrgPn="AVC_TS_HD_24_AC3_ISO,AVC_TS_HD_50_AC3_ISO,AVC_TS_HD_60_AC3_ISO,AVC_TS_HD_EU_ISO",
                    Type = DlnaProfileType.Video,

                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.PacketLength,
                            Value = "188"
                        }
                    }
                },

                new ResponseProfile
                {
                    Container = "ts,mpegts",
                    VideoCodec="h264",
                    AudioCodec="ac3,aac,mp3",
                    MimeType = "video/vnd.dlna.mpeg-tts",
                    OrgPn="AVC_TS_HD_24_AC3,AVC_TS_HD_50_AC3,AVC_TS_HD_60_AC3,AVC_TS_HD_EU",
                    Type = DlnaProfileType.Video
                },

                new ResponseProfile
                {
                    Container = "ts,mpegts",
                    VideoCodec="mpeg2video",
                    MimeType = "video/vnd.dlna.mpeg-tts",
                    OrgPn="MPEG_TS_SD_EU,MPEG_TS_SD_NA,MPEG_TS_SD_KO",
                    Type = DlnaProfileType.Video
                },

                new ResponseProfile
                {
                    Container = "mpeg",
                    VideoCodec="mpeg1video,mpeg2video",
                    MimeType = "video/mpeg",
                    OrgPn="MPEG_PS_NTSC,MPEG_PS_PAL",
                    Type = DlnaProfileType.Video
                },
                new ResponseProfile
                {
                    Container = "m4v",
                    Type = DlnaProfileType.Video,
                    MimeType = "video/mp4"
                }
            };


            CodecProfiles = new[]
            {
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
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoFramerate,
                            Value = "30"
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.VideoAudio,
                    Codec = "mp3,mp2",

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
                    Method = SubtitleDeliveryMethod.Embed
                }
            };
        }
    }
}
