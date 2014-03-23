using MediaBrowser.Controller.Dlna;

namespace MediaBrowser.Dlna.Profiles
{
    public class SonyBravia2010Profile : DefaultProfile
    {
        public SonyBravia2010Profile()
        {
            Name = "Sony Bravia (2010)";
            ClientType = "DLNA";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"KDL-\d{2}[EHLNPB]X\d[01]\d.*",
                Manufacturer = "Sony",

                Headers = new []
                {
                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Client-Info",
                        Value = @".*KDL-\d{2}[EHLNPB]X\d[01]\d.*",
                        Match = HeaderMatchType.Regex
                    }
                }
            };

            ModelName = "Windows Media Player Sharing";
            ModelNumber = "3.0";
            ModelUrl = "http://www.microsoft.com/";
            Manufacturer = "Microsoft Corporation";
            ManufacturerUrl = "http://www.microsoft.com/";
            SonyAggregationFlags = "10";
            ProtocolInfo =
                "http-get:*:audio/mpeg:DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000";

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
                    AudioCodec = "ac3,aac",
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
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "ac3,aac,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "ts",
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

            MediaProfiles = new[]
            {
                new MediaProfile
                {
                    Container = "avi",
                    MimeType = "video/avi",
                    Type = DlnaProfileType.Video
                },

                new MediaProfile
                {
                    Container = "asf",
                    MimeType = "video/x-ms-wmv",
                    Type = DlnaProfileType.Audio
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
                    Type = CodecType.VideoCodec,
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
                },

                new CodecProfile
                {
                    Type = CodecType.VideoCodec,
                    Codec = "h264",
                    Conditions = new []
                    {
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
                    Type = CodecType.VideoCodec,
                    Codec = "mpeg2video",
                    Conditions = new []
                    {
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
                    Type = CodecType.VideoAudioCodec,
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
                    Type = CodecType.VideoAudioCodec,
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
        }
    }
}
