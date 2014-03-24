using MediaBrowser.Controller.Dlna;

namespace MediaBrowser.Dlna.Profiles
{
    public class WdtvLiveProfile : DefaultProfile
    {
        public WdtvLiveProfile()
        {
            Name = "WDTV Live";

            TimelineOffsetSeconds = 5;
            IgnoreTranscodeByteRangeRequests = true;

            Identification = new DeviceIdentification
            {
                ModelName = "WD TV HD Live",

                Headers = new []
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
                    AudioCodec = "aac",

                    Settings = new []
                    {
                        new TranscodingSetting {Name = TranscodingSettingType.VideoLevel, Value = "3"},
                        new TranscodingSetting {Name = TranscodingSettingType.VideoProfile, Value = "baseline"}
                    }
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
                    AudioCodec = "ac3,dca,mp2,mp3,pcm"
                },

                new DirectPlayProfile
                {
                    Container = "mpeg",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video",
                    AudioCodec = "ac3,dca,mp2,mp3,pcm"
                },

                new DirectPlayProfile
                {
                    Container = "mkv",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video,mpeg4,h264,vc1",
                    AudioCodec = "ac3,dca,aac,mp2,mp3,pcm"
                },

                new DirectPlayProfile
                {
                    Container = "ts",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "mpeg1video,mpeg2video,h264,vc1",
                    AudioCodec = "ac3,dca,mp2,mp3"
                },

                new DirectPlayProfile
                {
                    Container = "mp4,mov",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "ac3,aac,mp2,mp3"
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
                    AudioCodec = "flac",
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

            MediaProfiles = new[]
            {
                new MediaProfile
                {
                    Container = "ts",
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
                    Type = CodecType.VideoCodec,
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
                    Type = CodecType.VideoAudioCodec,
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
        }
    }
}
