#pragma warning disable SA1118 // Parameter should not span multiple lines

using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="LgTvProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class LgTvProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LgTvProfile"/> class.
        /// </summary>
        public LgTvProfile()
        {
            Name = "LG Smart TV";

            TimelineOffsetSeconds = 10;

            Identification = new DeviceIdentification(
                @"LG.*",
                new[]
                {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "LG",
                       Match = HeaderMatchType.Substring
                   }
                });

            TranscodingProfiles = new[]
            {
               new TranscodingProfile("mp3", "mp3"),
               new TranscodingProfile("ts", "h264", "ac3,aac,mp3"),
               new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("ts,mpegts,avi,mkv,m2ts",  "h264", "aac,ac3,eac3,mp3,dca,dts"),
                new DirectPlayProfile("mp4,m4v", "h264,mpeg4", "aac,ac3,eac3,mp3,dca,dts"),
                new DirectPlayProfile("mp3", null),
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
                    "mpeg4",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30")
                    }),
                new CodecProfile(
                    "h264",
                    CodecType.Video,
                    new[]
                    {
                       new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                       new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                       new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoLevel, "41"),
                    }),
                new CodecProfile(
                    "ac3,eac3,aac,mp3",
                    CodecType.VideoAudio,
                    new[]
                    {
                       new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "6"),
                    })
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("srt", SubtitleDeliveryMethod.External)
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4"),
                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, "video/mpeg")
            };
        }
    }
}
