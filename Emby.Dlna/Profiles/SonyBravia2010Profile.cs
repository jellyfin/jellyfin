#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SonyBravia2010Profile : DefaultProfile
    {
        public SonyBravia2010Profile()
        {
            Name = "Sony Bravia (2010)";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"KDL-\d{2}[EHLNPB]X\d[01]\d.*",
                Manufacturer = "Sony",

                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Client-Info",
                        Value = @".*KDL-\d{2}[EHLNPB]X\d[01]\d.*",
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
            ProtocolInfo =
                "http-get:*:audio/mpeg:DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000";

            EnableSingleAlbumArtLimit = true;
            EnableAlbumArtInDidl = true;

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
                    AudioCodec = "ac3,aac,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "ts,mpegts",
                    VideoCodec = "mpeg1video,mpeg2video",
                    AudioCodec = "mp3,mp2",
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
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio
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
                            Value = "30"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitrate,
                            Value = "20000000"
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
                    Type = CodecType.Video,
                    Codec = "mpeg2video",
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
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitrate,
                            Value = "20000000"
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
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoFramerate,
                            Value = "30"
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
                            Value = "6"
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
