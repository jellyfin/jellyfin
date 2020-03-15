#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class LgTvProfile : DefaultProfile
    {
        public LgTvProfile()
        {
            Name = "LG Smart TV";

            TimelineOffsetSeconds = 10;

            Identification = new DeviceIdentification
            {
                FriendlyName = @"LG.*",

                Headers = new[]
               {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "LG",
                       Match = HeaderMatchType.Substring
                   }
               }
            };

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
                   AudioCodec = "ac3,aac,mp3",
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
                    Container = "ts,mpegts,avi,mkv,m2ts",
                    VideoCodec = "h264",
                    AudioCodec = "aac,ac3,eac3,mp3,dca,dts",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp4,m4v",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "aac,ac3,eac3,mp3,dca,dts",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp3",
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
                   Codec = "mpeg4",

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
                           Property = ProfileConditionValue.VideoFramerate,
                           Value = "30"
                       }
                   }
               },

               new CodecProfile
               {
                   Type = CodecType.Video,
                   Codec = "h264",

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
                           Property = ProfileConditionValue.VideoLevel,
                           Value = "41"
                       }
                   }
               },

               new CodecProfile
               {
                   Type = CodecType.VideoAudio,
                   Codec = "ac3,eac3,aac,mp3",

                   Conditions = new[]
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
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.Embed
                },
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.External
                }
            };

            ResponseProfiles = new ResponseProfile[]
            {
                new ResponseProfile
                {
                    Container = "m4v",
                    Type = DlnaProfileType.Video,
                    MimeType = "video/mp4"
                },
                new ResponseProfile
                {
                    Container = "ts,mpegts",
                    Type = DlnaProfileType.Video,
                    MimeType = "video/mpeg"
                }
            };
        }
    }
}
