#pragma warning disable SA1118 // Parameter should not span multiple lines
using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="PanasonicVieraProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class PanasonicVieraProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PanasonicVieraProfile"/> class.
        /// </summary>
        public PanasonicVieraProfile()
        {
            Name = "Panasonic Viera";

            Identification = new DeviceIdentification(
                "VIERA",
                new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = "Panasonic MIL DLNA",
                        Match = HeaderMatchType.Substring
                    }
                })
            {
                Manufacturer = "Panasonic"
            };

            AddXmlRootAttribute("xmlns:pv", "http://www.pv.com/pvns/");

            TimelineOffsetSeconds = 10;

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3"),
                new TranscodingProfile("ts", "h264", "ac3"),
                new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("mpeg,mpg", "mpeg2video,mpeg4", "ac3,mp3,pcm_dvd"),
                new DirectPlayProfile("mkv", "h264,mpeg2video", "aac,ac3,dca,mp3,mp2,pcm,dts"),
                new DirectPlayProfile("ts,mpegts", "h264,mpeg2video", "aac,mp3,mp2"),
                new DirectPlayProfile("mp4,m4v", "h264", "aac,ac3,mp3,pcm"),
                new DirectPlayProfile("mov", "h264", "aac,pcm"),
                new DirectPlayProfile("avi", "mpeg4", "pcm"),
                new DirectPlayProfile("flv", "h264", "aac"),
                new DirectPlayProfile("mp3", "mp3"),
                new DirectPlayProfile("mp4", "aac"),
                new DirectPlayProfile("jpeg")
            };

            ContainerProfiles = new[]
            {
                new ContainerProfile(
                    DlnaProfileType.Photo,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080")
                    })
            };

            CodecProfiles = new[]
            {
                new CodecProfile(
                    null,
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitDepth, "8", false)
                    })
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("srt", SubtitleDeliveryMethod.External)
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, "video/vnd.dlna.mpeg-tts")
                {
                    OrgPn = "MPEG_TS_SD_EU,MPEG_TS_SD_NA,MPEG_TS_SD_KO",
                },

                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4")
            };
        }
    }
}
