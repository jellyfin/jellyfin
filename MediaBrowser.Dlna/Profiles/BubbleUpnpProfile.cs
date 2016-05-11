using MediaBrowser.Model.Dlna;
using System.Xml.Serialization;

namespace MediaBrowser.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class BubbleUpnpProfile : DefaultProfile
    {
        public BubbleUpnpProfile()
        {
            Name = "BubbleUPnp";

            Identification = new DeviceIdentification
            {
                ModelName = "BubbleUPnp",

                Headers = new[]
                {
                    new HttpHeaderInfo {Name = "User-Agent", Value = "BubbleUPnp", Match = HeaderMatchType.Substring}
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
                    Type = DlnaProfileType.Video,
                    AudioCodec = "aac",
                    VideoCodec = "h264"
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
                    Container = "",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "",
                    Type = DlnaProfileType.Photo,
                }
            };

            ResponseProfiles = new ResponseProfile[] { };

            ContainerProfiles = new ContainerProfile[] { };

            CodecProfiles = new CodecProfile[] { };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.External,
                },

                new SubtitleProfile
                {
                    Format = "sub",
                    Method = SubtitleDeliveryMethod.External,
                },

                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.Embed,
                    DidlMode = "",
                },

                new SubtitleProfile
                {
                    Format = "ass",
                    Method = SubtitleDeliveryMethod.Embed,
                    DidlMode = "",
                },

                new SubtitleProfile
                {
                    Format = "ssa",
                    Method = SubtitleDeliveryMethod.Embed,
                    DidlMode = "",
                },

                new SubtitleProfile
                {
                    Format = "smi",
                    Method = SubtitleDeliveryMethod.Embed,
                    DidlMode = "",
                },

                new SubtitleProfile
                {
                    Format = "dvdsub",
                    Method = SubtitleDeliveryMethod.Embed,
                    DidlMode = "",
                },

                new SubtitleProfile
                {
                    Format = "pgs",
                    Method = SubtitleDeliveryMethod.Embed,
                    DidlMode = "",
                },

                new SubtitleProfile
                {
                    Format = "pgssub",
                    Method = SubtitleDeliveryMethod.Embed,
                    DidlMode = "",
                },

                new SubtitleProfile
                {
                    Format = "sub",
                    Method = SubtitleDeliveryMethod.Embed,
                    DidlMode = "",
                }
            };
        }
    }
}
