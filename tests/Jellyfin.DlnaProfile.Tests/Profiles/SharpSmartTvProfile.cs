#pragma warning disable SA1118 // Parameter should not span multiple lines
using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="SharpSmartTvProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SharpSmartTvProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharpSmartTvProfile"/> class.
        /// </summary>
        public SharpSmartTvProfile()
        {
            Name = "Sharp Smart TV";

            RequiresPlainFolders = true;
            RequiresPlainVideoItems = true;

            Identification = new DeviceIdentification(
                "Sharp Smart TV",
                new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = "Sharp",
                        Match = HeaderMatchType.Substring
                    }
                })
            {
                Manufacturer = "Sharp",
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3"),
                new TranscodingProfile("ts", "h264", "ac3,aac,mp3,dts,dca")
                {
                    EnableMpegtsM2TsMode = true
                },
                new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("m4v,mkv,avi,mov,mp4", "h264,mpeg4", "aac,mp3,ac3,dts,dca"),
                new DirectPlayProfile("asf,wmv", null, null),
                new DirectPlayProfile("mpg,mpeg", "mpeg2video", "mp3,aac"),
                new DirectPlayProfile("flv", "h264", "mp3,aac"),
                new DirectPlayProfile("mp3,wav", null)
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("srt", SubtitleDeliveryMethod.External)
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4")
            };
        }
    }
}
