using System.Linq;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    /// <summary>
    /// Defines the <see cref="DefaultProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class DefaultProfile : DeviceProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultProfile"/> class.
        /// </summary>
        public DefaultProfile()
        {
            Name = "Generic Device";

            ProtocolInfo = "http-get:*:video/mpeg:*,http-get:*:video/mp4:*,http-get:*:video/vnd.dlna.mpeg-tts:*,http-get:*:video/avi:*,http-get:*:video/x-matroska:*,http-get:*:video/x-ms-wmv:*,http-get:*:video/wtv:*,http-get:*:audio/mpeg:*,http-get:*:audio/mp3:*,http-get:*:audio/mp4:*,http-get:*:audio/x-ms-wma:*,http-get:*:audio/wav:*,http-get:*:audio/L16:*,http-get:*:image/jpeg:*,http-get:*:image/png:*,http-get:*:image/gif:*,http-get:*:image/tiff:*";

            Manufacturer = "Jellyfin";
            ModelDescription = "UPnP/AV 1.0 Compliant Media Server";
            ModelName = "Jellyfin Server";
            ModelNumber = "01";
            ModelUrl = "https://github.com/jellyfin/jellyfin";
            ManufacturerUrl = "https://github.com/jellyfin/jellyfin";

            AlbumArtPn = "JPEG_SM";

            MaxAlbumArtHeight = 480;
            MaxAlbumArtWidth = 480;

            MaxIconWidth = 48;
            MaxIconHeight = 48;

            MaxStreamingBitrate = 140000000;
            MaxStaticBitrate = 140000000;
            MusicStreamingTranscodingBitrate = 192000;

            EnableAlbumArtInDidl = false;

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3"),
                new TranscodingProfile("ts", "aac", "h264"),
                new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile(string.Empty)
                {
                    // play all
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile(string.Empty)
                {
                    // play all
                    Type = DlnaProfileType.Audio
                }
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.External),
                new SubtitleProfile("sub", SubtitleDeliveryMethod.External),
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("ass", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("ssa", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("smi", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("dvdsub", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("pgs", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("pgssub", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("sub", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("subrip", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("vtt", SubtitleDeliveryMethod.Embed)
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4")
            };
        }

        /// <summary>
        /// The AddXmlRootAttribute.
        /// </summary>
        /// <param name="name">The name<see cref="string"/>.</param>
        /// <param name="value">The value<see cref="string"/>.</param>
        public void AddXmlRootAttribute(string name, string value)
        {
            var atts = XmlRootAttributes ?? System.Array.Empty<XmlAttribute>();
            var list = atts.ToList();

            list.Add(new XmlAttribute(name, value));

            XmlRootAttributes = list.ToArray();
        }
    }
}
