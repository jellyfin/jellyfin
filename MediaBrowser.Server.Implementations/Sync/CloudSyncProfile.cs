using MediaBrowser.Model.Dlna;
using System.Collections.Generic;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class CloudSyncProfile : DeviceProfile
    {
        public CloudSyncProfile(bool supportsAc3, bool supportsDca)
        {
            Name = "Cloud Sync";

            MaxStreamingBitrate = 20000000;
            MaxStaticBitrate = 20000000;

            var mkvAudio = "aac,mp3";
            var mp4Audio = "aac";

            if (supportsAc3)
            {
                mkvAudio += ",ac3";
                mp4Audio += ",ac3";
            }

            if (supportsDca)
            {
                mkvAudio += ",dca,dts";
            }

            var videoProfile = "high|main|baseline|constrained baseline";
            var videoLevel = "40";

            DirectPlayProfiles = new[]
            {
                //new DirectPlayProfile
                //{
                //    Container = "mkv",
                //    VideoCodec = "h264,mpeg4",
                //    AudioCodec = mkvAudio,
                //    Type = DlnaProfileType.Video
                //},
                new DirectPlayProfile
                {
                    Container = "mp4,mov,m4v",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = mp4Audio,
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp3",
                    Type = DlnaProfileType.Audio
                }
            };

            ContainerProfiles = new[]
            {
                new ContainerProfile
                { 
                    Type = DlnaProfileType.Video,
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.NotEquals,
                            Property = ProfileConditionValue.NumAudioStreams,
                            Value = "0",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.EqualsAny,
                            Property = ProfileConditionValue.NumVideoStreams,
                            Value = "1",
                            IsRequired = false
                        }
                    }
                }
            };

            var codecProfiles = new List<CodecProfile>
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
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Width,
                            Value = "1920",
                            IsRequired = true
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080",
                            IsRequired = true
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.RefFrames,
                            Value = "4",
                            IsRequired = false
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
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.IsAnamorphic,
                            Value = "false",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoLevel,
                            Value = videoLevel,
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.EqualsAny,
                            Property = ProfileConditionValue.VideoProfile,
                            Value = videoProfile,
                            IsRequired = false
                        }
                    }
                },
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec = "mpeg4",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Width,
                            Value = "1920",
                            IsRequired = true
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080",
                            IsRequired = true
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.RefFrames,
                            Value = "4",
                            IsRequired = false
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
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.IsAnamorphic,
                            Value = "false",
                            IsRequired = false
                        }
                    }
                }
            };

            codecProfiles.Add(new CodecProfile
            {
                Type = CodecType.VideoAudio,
                Codec = "ac3",
                Conditions = new[]
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
                            Value = "320000",
                            IsRequired = true
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.IsSecondaryAudio,
                            Value = "false",
                            IsRequired = false
                        }
                    }
            });
            codecProfiles.Add(new CodecProfile
            {
                Type = CodecType.VideoAudio,
                Codec = "aac,mp3",
                Conditions = new[]
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioChannels,
                            Value = "2",
                            IsRequired = true
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioBitrate,
                            Value = "320000",
                            IsRequired = true
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.Equals,
                            Property = ProfileConditionValue.IsSecondaryAudio,
                            Value = "false",
                            IsRequired = false
                        }
                    }
            });

            CodecProfiles = codecProfiles.ToArray();

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.External
                },
                new SubtitleProfile
                {
                    Format = "vtt",
                    Method = SubtitleDeliveryMethod.External
                }
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio,
                    Context = EncodingContext.Static
                },

                new TranscodingProfile
                {
                    Container = "mp4",
                    Type = DlnaProfileType.Video,
                    AudioCodec = "aac",
                    VideoCodec = "h264",
                    Context = EncodingContext.Static
                },

                new TranscodingProfile
                {
                    Container = "jpeg",
                    Type = DlnaProfileType.Photo,
                    Context = EncodingContext.Static
                }
            };

        }
    }
}
