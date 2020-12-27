#pragma warning disable SA1118 // Parameter should not span multiple lines
using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="DishHopperJoeyProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class DishHopperJoeyProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DishHopperJoeyProfile"/> class.
        /// </summary>
        public DishHopperJoeyProfile()
        {
            Name = "Dish Hopper-Joey";

            ProtocolInfo = "http-get:*:video/mp2t:*,http-get:*:video/mpeg:*,http-get:*:video/MP1S:*,http-get:*:video/mpeg2:*,http-get:*:video/mp4:*,http-get:*:video/x-matroska:*,http-get:*:audio/mpeg:*,http-get:*:audio/mpeg3:*,http-get:*:audio/mp3:*,http-get:*:audio/mp4:*,http-get:*:audio/mp4a-latm:*,http-get:*:image/jpeg:*";

            Identification = new DeviceIdentification(
                null,
                new[]
                {
                    new HttpHeaderInfo
                    {
                         Match = HeaderMatchType.Substring,
                         Name = "User-Agent",
                         Value = "Zip_"
                    }
                })
            {
                Manufacturer = "Echostar Technologies LLC",
                ManufacturerUrl = "http://www.echostar.com",
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3"),
                new TranscodingProfile("mp4", "h264", "aac"),
                new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("mp4,mkv,mpeg,ts", "h264,mpeg2video", "mp3,ac3,aac,he-aac,pcm"),
                new DirectPlayProfile("mp3,alac,flac", null),
                new DirectPlayProfile("jpeg")
            };

            CodecProfiles = new[]
            {
                new CodecProfile(
                    "h264",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920", true),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080", true),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30", true),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "20000000", true),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoLevel, "41", true)
                    }),

                new CodecProfile(
                    null,
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920", true),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080", true),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30", true),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "20000000", true),
                    }),

                new CodecProfile(
                    "ac3,he-aac",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "6", true)
                    }),

                new CodecProfile(
                    "aac",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2", true)
                    }),

                new CodecProfile(
                    null,
                    CodecType.VideoAudio,
                    new[]
                    {
                        // The device does not have any audio switching capabilities
                        new ProfileCondition(ProfileConditionType.Equals, ProfileConditionValue.IsSecondaryAudio, "false")
                    })
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("mkv,ts,mpegts", DlnaProfileType.Video, "video/mp4")
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed)
            };
        }
    }
}
