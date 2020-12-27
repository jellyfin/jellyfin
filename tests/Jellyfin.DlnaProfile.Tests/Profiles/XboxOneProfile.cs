using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="XboxOneProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class XboxOneProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XboxOneProfile"/> class.
        /// </summary>
        public XboxOneProfile()
        {
            Name = "Xbox One";

            TimelineOffsetSeconds = 40;

            Identification = new DeviceIdentification(
                null,
                new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "FriendlyName.DLNA.ORG", Value = "XboxOne", Match = HeaderMatchType.Substring
                    },
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent", Value = "NSPlayer/12", Match = HeaderMatchType.Substring
                    }
                })
            {
                ModelName = Name
            };

            var videoProfile = "high|main|baseline|constrained baseline";
            var videoLevel = "41";

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3"),
                new TranscodingProfile("jpeg")
                {
                    VideoCodec = "jpeg"
                },

                new TranscodingProfile("ts", "h264", "aac")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("ts,mpegts", "h264,mpeg2video,hevc", "ac3,aac,mp3"),
                new DirectPlayProfile("avi", "mpeg4", "ac3,mp3"),
                new DirectPlayProfile("avi", "h264", "aac"),
                new DirectPlayProfile("mp4,mov,mkv,m4v", "h264,mpeg4,mpeg2video,hevc", "aac,ac3"),
                new DirectPlayProfile("asf", "wmv2,wmv3,vc1", "wmav2,wmapro"),
                new DirectPlayProfile("asf", "wmav2,wmapro,wmavoice"),
                new DirectPlayProfile("mp3", "mp3"),
                new DirectPlayProfile("jpeg")
            };

            ContainerProfiles = new[]
            {
                new ContainerProfile(
                    DlnaProfileType.Video,
                    "mp4,mov",
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.Equals, ProfileConditionValue.Has64BitOffsets, "false", false)
                    })
            };

            CodecProfiles = new[]
            {
                new CodecProfile(
                    "mpeg4",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.IsAnamorphic, "true", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitDepth, "8", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "5120000", false)
                    }),

                new CodecProfile(
                    "h264",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.IsAnamorphic, "true", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitDepth, "8", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoLevel, videoLevel, false),
                        new ProfileCondition(ProfileConditionType.EqualsAny, ProfileConditionValue.VideoProfile, videoProfile, false)
                    }),

                new CodecProfile(
                    "wmv2,wmv3,vc1",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.IsAnamorphic, "true", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitDepth, "8", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "15360000", false),
                    }),

                new CodecProfile(
                    null,
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.IsAnamorphic, "true", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitDepth, "8", false)
                    }),

                new CodecProfile(
                    "ac3,wmav2,wmapro",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "6", false)
                    }),

                new CodecProfile(
                    "aac",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2", false),
                        new ProfileCondition(ProfileConditionType.Equals, ProfileConditionValue.AudioProfile, "lc", false)
                    })
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("avi", DlnaProfileType.Video, "video/avi"),
                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4")
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed)
            };
        }
    }
}
