using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class BubbleUpnpProfile : DefaultProfile
    {
        public BubbleUpnpProfile()
        {
            Name = "BubbleUPnp";

            TimelineOffsetSeconds = 5;

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
                    Type = DlnaProfileType.Audio,
                    AudioCodec = "mp3"
                },
                new TranscodingProfile
                {
                    Container = "ts",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264",
                    AudioCodec = "aac"
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
                    Container = "avi,mpeg,mkv,ts,mp4,mov,m4v,asf,webm,ogg,ogv,iso",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mp3,flac,asf,off,oga,aac",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Type = DlnaProfileType.Photo,

                    Container = "jpeg,png,gif,bmp,tiff"
                }
            };

            ResponseProfiles = new ResponseProfile[] { };

            ContainerProfiles = new ContainerProfile[] { };

            CodecProfiles = new CodecProfile[] { };
        }
    }
}
