#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SamsungSmartTvProfile : DefaultProfile
    {
        public SamsungSmartTvProfile()
        {
            Name = "Samsung Smart TV";

            EnableAlbumArtInDidl = true;

            // Without this, older samsungs fail to browse
            EnableSingleAlbumArtLimit = true;

            Identification = new DeviceIdentification
            {
                ModelUrl = "samsung.com",

                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = @"SEC_",
                        Match = HeaderMatchType.Substring
                    }
                }
            };

            AddXmlRootAttribute("xmlns:sec", "http://www.sec.co.kr/");

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
                   Type = DlnaProfileType.Video,
                   EstimateContentLength = false
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
                    Container = "asf",
                    VideoCodec = "h264,mpeg4,mjpeg",
                    AudioCodec = "mp3,ac3,wmav2,wmapro,wmavoice",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "avi",
                    VideoCodec = "h264,mpeg4,mjpeg",
                    AudioCodec = "mp3,ac3,dca,dts",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mkv",
                    VideoCodec = "h264,mpeg4,mjpeg4",
                    AudioCodec = "mp3,ac3,dca,aac,dts",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp4,m4v",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "mp3,aac",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "3gp",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "aac,he-aac",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mpg,mpeg",
                    VideoCodec = "mpeg1video,mpeg2video,h264",
                    AudioCodec = "ac3,mp2,mp3,aac",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "vro,vob",
                    VideoCodec = "mpeg1video,mpeg2video",
                    AudioCodec = "ac3,mp2,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "ts",
                    VideoCodec = "mpeg2video,h264,vc1",
                    AudioCodec = "ac3,aac,mp3,eac3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "asf",
                    VideoCodec = "wmv2,wmv3",
                    AudioCodec = "wmav2,wmavoice",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp3,flac",
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
                   Codec = "mpeg2video",

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
                       },
                       new ProfileCondition
                       {
                           Condition = ProfileConditionType.LessThanEqual,
                           Property = ProfileConditionValue.VideoBitrate,
                           Value = "30720000"
                       }
                   }
               },

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
                       },
                       new ProfileCondition
                       {
                           Condition = ProfileConditionType.LessThanEqual,
                           Property = ProfileConditionValue.VideoBitrate,
                           Value = "8192000"
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
                           Property = ProfileConditionValue.VideoFramerate,
                           Value = "30"
                       },
                       new ProfileCondition
                       {
                           Condition = ProfileConditionType.LessThanEqual,
                           Property = ProfileConditionValue.VideoBitrate,
                           Value = "37500000"
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
                   Codec = "wmv2,wmv3,vc1",

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
                       },
                       new ProfileCondition
                       {
                           Condition = ProfileConditionType.LessThanEqual,
                           Property = ProfileConditionValue.VideoBitrate,
                           Value = "25600000"
                       }
                   }
               },

               new CodecProfile
               {
                   Type = CodecType.VideoAudio,
                   Codec = "wmav2,dca,aac,mp3,dts",

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

            ResponseProfiles = new[]
            {
                new ResponseProfile
                {
                    Container = "avi",
                    MimeType = "video/x-msvideo",
                    Type = DlnaProfileType.Video
                },

                new ResponseProfile
                {
                    Container = "mkv",
                    MimeType = "video/x-mkv",
                    Type = DlnaProfileType.Video
                },

                new ResponseProfile
                {
                    Container = "flac",
                    MimeType = "audio/x-flac",
                    Type = DlnaProfileType.Audio
                },
                new ResponseProfile
                {
                    Container = "m4v",
                    Type = DlnaProfileType.Video,
                    MimeType = "video/mp4"
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
                    Method = SubtitleDeliveryMethod.External,
                    DidlMode = "CaptionInfoEx"
                }
            };
        }
    }
}
