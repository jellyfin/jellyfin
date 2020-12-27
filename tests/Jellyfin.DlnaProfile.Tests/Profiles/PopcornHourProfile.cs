#pragma warning disable SA1118 // Parameter should not span multiple lines
using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="PopcornHourProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class PopcornHourProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PopcornHourProfile"/> class.
        /// </summary>
        public PopcornHourProfile()
        {
            Name = "Popcorn Hour";

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3"),
                new TranscodingProfile("mp4", "h264", "aac"),
                new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("mp4,mov,m4v", "h264,mpeg4", "aac"),
                new DirectPlayProfile("ts,mpegts", "h264", "aac,ac3,eac3,mp3,mp2,pcm"),
                new DirectPlayProfile("asf,wmv", "wmv3,vc1", "wmav2,wmapro"),
                new DirectPlayProfile("avi", "mpeg4,msmpeg4", "mp3,ac3,eac3,mp2,pcm"),
                new DirectPlayProfile("mkv", "h264", "aac,mp3,ac3,eac3,mp2,pcm"),
                new DirectPlayProfile("aac,mp3,flac,ogg,wma,wav", null),
                new DirectPlayProfile("jpeg,gif,bmp,png")
            };

            CodecProfiles = new[]
            {
                new CodecProfile(
                    "h264",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.EqualsAny, ProfileConditionValue.VideoProfile, "baseline|constrained baseline", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.IsAnamorphic, "true", false),
                    }),

                new CodecProfile(
                    null,
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.IsAnamorphic, "true", false),
                    }),

                new CodecProfile(
                    "aac",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2", false)
                    }),

                new CodecProfile(
                    "aac",
                    CodecType.Audio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2", false)
                    }),

                new CodecProfile(
                    "mp3",
                    CodecType.Audio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioBitrate, "320000", false)
                    })
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
