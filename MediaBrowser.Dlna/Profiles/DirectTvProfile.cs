using MediaBrowser.Model.Dlna;
using System.Xml.Serialization;

namespace MediaBrowser.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class DirectTvProfile : DefaultProfile
    {
        public DirectTvProfile()
        {
            Name = "DirecTV HD-DVR";

            TimelineOffsetSeconds = 10;
            RequiresPlainFolders = true;
            RequiresPlainVideoItems = true;

            Identification = new DeviceIdentification
            {
                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                         Match = HeaderMatchType.Substring,
                         Name = "User-Agent",
                         Value = "DIRECTV"
                    }
                },

                FriendlyName = "^DIRECTV.*$"
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mpeg",
                    VideoCodec = "mpeg2video",
                    AudioCodec = "mp2",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "jpeg,jpg",
                    Type = DlnaProfileType.Photo
                }
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mpeg",
                    VideoCodec = "mpeg2video",
                    AudioCodec = "mp2",
                    Type = DlnaProfileType.Video
                },
                new TranscodingProfile
                {
                    Container = "jpeg",
                    Type = DlnaProfileType.Photo
                }
            };

            CodecProfiles = new[]
            {
                new CodecProfile
                {
                    Codec = "mpeg2video",
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
                    Codec = "mp2",
                    Type = CodecType.Audio,

                    Conditions = new[]
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

            ResponseProfiles = new ResponseProfile[] { };
        }
    }
}
