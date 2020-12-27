#pragma warning disable SA1118 // Parameter should not span multiple lines
using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="SonyPs4Profile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SonyPs4Profile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SonyPs4Profile"/> class.
        /// </summary>
        public SonyPs4Profile()
        {
            Name = "Sony PlayStation 4";

            var headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = @"PLAYSTATION 4",
                        Match = HeaderMatchType.Substring
                    },

                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Client-Info",
                        Value = @"PLAYSTATION 4",
                        Match = HeaderMatchType.Substring
                    }
                };

            Identification = new DeviceIdentification(
                "PLAYSTATION 4",
                headers);

            AlbumArtPn = "JPEG_TN";

            SonyAggregationFlags = "10";
            EnableSingleAlbumArtLimit = true;

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("avi", "mpeg4", "mp2,mp3"),
                new DirectPlayProfile("ts,mpegts", "mpeg1video,mpeg2video,h264", "aac,ac3,mp2"),
                new DirectPlayProfile("mpeg", "mpeg1video,mpeg2video", "mp2"),
                new DirectPlayProfile("mp4,mkv,m4v", "h264,mpeg4", "aac,ac3"),
                new DirectPlayProfile("aac,mp3,wav", null),
                new DirectPlayProfile("jpeg,png,gif,bmp,tiff")
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3")
                {
                    // Transcoded audio won't be playable at all without this
                    TranscodeSeekInfo = TranscodeSeekInfo.Bytes
                },
                new TranscodingProfile("ts", "h264", "aac,ac3,mp2"),
                new TranscodingProfile("jpeg")
            };

            ContainerProfiles = new[]
            {
                new ContainerProfile(
                    DlnaProfileType.Photo,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920")
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
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "15360000", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoLevel, "41", false)
                    }),

                new CodecProfile(
                    "ac3",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "6", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioBitrate, "640000", false)
                    }),

                new CodecProfile(
                    "wmapro",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2")
                    }),

                new CodecProfile(
                    "aac",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.AudioProfile, "he-aac", false)
                    })
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("mp4,mov", DlnaProfileType.Video, "video/mp4")
                {
                    AudioCodec = "aac",
                },

                new ResponseProfile("avi", DlnaProfileType.Video, "video/divx")
                {
                    OrgPn = "AVI",
                },

                new ResponseProfile("wav", DlnaProfileType.Audio, "audio/wav"),
                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4")
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed)
            };
        }
    }
}
