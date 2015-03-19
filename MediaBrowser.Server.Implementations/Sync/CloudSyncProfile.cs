using MediaBrowser.Model.Dlna;

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
                mkvAudio += ",dca";
            }

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mkv",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = mkvAudio,
                    Type = DlnaProfileType.Video
                },
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

            ContainerProfiles = new ContainerProfile[] { };

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
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.RefFrames,
                            Value = "12",
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
