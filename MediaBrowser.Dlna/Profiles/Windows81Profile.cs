using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dlna.Profiles;
using System.Xml.Serialization;

namespace MediaBrowser.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class Windows81Profile : DefaultProfile
    {
        public Windows81Profile()
        {
            Name = "Windows 8/RT";

            Identification = new DeviceIdentification
            {
                Manufacturer = "Microsoft SDK Customer"
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio,
                    Context = EncodingContext.Streaming
                },
                new TranscodingProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio,
                    Context = EncodingContext.Static
                },
                new TranscodingProfile
                {
                    Protocol = "hls",
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Video,
                    Context = EncodingContext.Streaming
                },
                new TranscodingProfile
                {
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Video,
                    Context = EncodingContext.Streaming
                },
                new TranscodingProfile
                {
                    Container = "mp4",
                    VideoCodec = "h264",
                    AudioCodec = "aac,ac3,eac3",
                    Type = DlnaProfileType.Video,
                    Context = EncodingContext.Static
                }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mp4,mov",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "aac,ac3,eac3,mp3,pcm",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "aac,ac3,eac3,mp3,mp2,pcm",
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
                    Container = "avi",
                    VideoCodec = "mpeg4,msmpeg4,mjpeg",
                    AudioCodec = "mp3,ac3,eac3,mp2,pcm",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mp4",
                    AudioCodec = "aac",
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

            CodecProfiles = new[]
            {
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec="h264",
                    Conditions = new []
                    {
                        
                        // Note: Add any of the following if supported

                                        //"Constrained Baseline",
                                        //"Baseline",
                                        //"Extended",
                                        //"Main",
                                        //"High",
                                        //"Progressive High",
                                        //"Constrained High"

                        // The first one in the list should be the higest one, e.g. if High profile is supported, make sure it appears before baseline: high|baseline

                        new ProfileCondition(ProfileConditionType.EqualsAny, ProfileConditionValue.VideoProfile, "high|main|extended|baseline|constrained baseline"),
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoLevel,
                            Value = "51"
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
                    Type = CodecType.Video,
                    Conditions = new []
                    {
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
                    Codec = "aac,eac3",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioChannels,
                            Value = "8"
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
                }
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "vtt",
                    Method = SubtitleDeliveryMethod.External
                }
            };
        }
    }
}
