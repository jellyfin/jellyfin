#pragma warning disable SA1118 // Parameter should not span multiple lines

using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="DirectTvProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class DirectTvProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectTvProfile"/> class.
        /// </summary>
        public DirectTvProfile()
        {
            Name = "DirecTV HD-DVR";
            TimelineOffsetSeconds = 10;
            RequiresPlainFolders = true;
            RequiresPlainVideoItems = true;

            Identification = new DeviceIdentification(
                "^DIRECTV.*$",
                new[]
                {
                    new HttpHeaderInfo
                    {
                         Match = HeaderMatchType.Substring,
                         Name = "User-Agent",
                         Value = "DIRECTV"
                    }
                });

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("mpeg", "mpeg2video", "mp2"),
                new DirectPlayProfile("jpeg,jpg")
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mpeg", "mpeg2video", "mp2"),
                new TranscodingProfile("jpeg")
            };

            CodecProfiles = new[]
            {
                new CodecProfile(
                    "mpeg2video",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "8192000")
                    }),
                new CodecProfile(
                    "mp2",
                    CodecType.Audio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2")
                    })
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed)
            };

            ResponseProfiles = System.Array.Empty<ResponseProfile>();
        }
    }
}
