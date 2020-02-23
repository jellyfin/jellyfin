#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class LinksysDMA2100Profile : DefaultProfile
    {
        public LinksysDMA2100Profile()
        {
            // Linksys DMA2100us does not need any transcoding of the formats we support statically
            Name = "Linksys DMA2100";

            Identification = new DeviceIdentification
            {
                ModelName = "DMA2100us"
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mp3,flac,m4a,wma",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "avi,mp4,mkv,ts,mpegts,m4v",
                    Type = DlnaProfileType.Video
                }
            };

            ResponseProfiles = new ResponseProfile[]
            {
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
                }
            };
        }
    }
}
