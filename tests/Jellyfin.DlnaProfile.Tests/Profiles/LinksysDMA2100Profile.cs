using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="LinksysDMA2100Profile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class LinksysDMA2100Profile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinksysDMA2100Profile"/> class.
        /// </summary>
        public LinksysDMA2100Profile()
        {
            // Linksys DMA2100us does not need any transcoding of the formats we support statically
            Name = "Linksys DMA2100";

            Identification = new DeviceIdentification(null)
            {
                ModelName = "DMA2100us"
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("mp3,flac,m4a,wma", null),
                new DirectPlayProfile("avi,mp4,mkv,ts,mpegts,m4v", null)
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4")
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed)
            };
        }
    }
}
