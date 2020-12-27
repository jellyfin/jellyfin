#pragma warning disable SA1118 // Parameter should not span multiple lines
using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="WdtvLiveProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class WdtvLiveProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WdtvLiveProfile"/> class.
        /// </summary>
        public WdtvLiveProfile()
        {
            Name = "WDTV Live";

            TimelineOffsetSeconds = 5;
            IgnoreTranscodeByteRangeRequests = true;

            Identification = new DeviceIdentification(
                null,
                new[]
                {
                    new HttpHeaderInfo { Name = "User-Agent", Value = "alphanetworks", Match = HeaderMatchType.Substring },
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = "ALPHA Networks",
                        Match = HeaderMatchType.Substring
                    }
                });

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3"),
                new TranscodingProfile("ts", "h264", "aac"),
                new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("avi", "mpeg1video,mpeg2video,mpeg4,h264,vc1", "ac3,eac3,dca,mp2,mp3,pcm,dts"),
                new DirectPlayProfile("mpeg", "mpeg1video,mpeg2video", "ac3,eac3,dca,mp2,mp3,pcm,dts"),
                new DirectPlayProfile("mkv", "mpeg1video,mpeg2video,mpeg4,h264,vc1", "ac3,eac3,dca,aac,mp2,mp3,pcm,dts"),
                new DirectPlayProfile("ts,m2ts,mpegts", "mpeg1video,mpeg2video,h264,vc1", "ac3,eac3,dca,mp2,mp3,aac,dts"),
                new DirectPlayProfile("mp4,mov,m4v", "h264,mpeg4", "ac3,eac3,aac,mp2,mp3,dca,dts"),
                new DirectPlayProfile("asf", "vc1", "wmav2,wmapro"),
                new DirectPlayProfile("asf", "mpeg2video", "mp2,ac3"),
                new DirectPlayProfile("mp3", "mp2,mp3"),
                new DirectPlayProfile("mp4", "mp4"),
                new DirectPlayProfile("flac", null),
                new DirectPlayProfile("asf", "wmav2,wmapro,wmavoice"),
                new DirectPlayProfile("ogg", "vorbis"),
                new DirectPlayProfile("jpeg,png,gif,bmp,tiff")
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, null)
                {
                    OrgPn = "MPEG_TS_SD_NA"
                }
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
                    "h264",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoLevel, "41")
                    }),

                new CodecProfile(
                    "aac",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2")
                    })
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.External),
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("sub", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("subrip", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("idx", SubtitleDeliveryMethod.Embed)
            };
        }
    }
}
