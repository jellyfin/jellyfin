using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="SamsungSmartTvProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SamsungSmartTvProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamsungSmartTvProfile"/> class.
        /// </summary>
        public SamsungSmartTvProfile()
        {
            Name = "Samsung Smart TV";

            EnableAlbumArtInDidl = true;

            // Without this, older samsungs fail to browse
            EnableSingleAlbumArtLimit = true;

            Identification = new DeviceIdentification(
                null,
                new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "User-Agent",
                        Value = @"SEC_",
                        Match = HeaderMatchType.Substring
                    }
                })
            {
                ModelUrl = "samsung.com",
            };

            AddXmlRootAttribute("xmlns:sec", "http://www.sec.co.kr/");

            TranscodingProfiles = new[]
            {
               new TranscodingProfile("mp3", "mp3"),
               new TranscodingProfile("ts", "h264", "ac3")
               {
                   EstimateContentLength = false
               },
               new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("asf", "h264,mpeg4,mjpeg", "mp3,ac3,wmav2,wmapro,wmavoice"),
                new DirectPlayProfile("avi", "h264,mpeg4,mjpeg", "mp3,ac3,dca,dts"),
                new DirectPlayProfile("mkv", "h264,mpeg4,mjpeg4", "mp3,ac3,dca,aac,dts"),
                new DirectPlayProfile("mp4,m4v", "h264,mpeg4", "mp3,aac"),
                new DirectPlayProfile("3gp", "h264,mpeg4", "aac,he-aac"),
                new DirectPlayProfile("mpg,mpeg", "mpeg1video,mpeg2video,h264", "ac3,mp2,mp3,aac"),
                new DirectPlayProfile("vro,vob", "mpeg1video,mpeg2video", "ac3,mp2,mp3"),
                new DirectPlayProfile("ts", "mpeg2video,h264,vc1", "ac3,aac,mp3,eac3"),
                new DirectPlayProfile("asf", "wmv2,wmv3", "wmav2,wmavoice"),
                new DirectPlayProfile("mp3,flac", null),
                new DirectPlayProfile("jpeg")
            };

            ContainerProfiles = new[]
            {
                new ContainerProfile(
                    DlnaProfileType.Photo,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                    })
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
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "30720000")
                    }),

                new CodecProfile(
                    "mpeg4",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "8192000"),
                    }),

                new CodecProfile(
                   "h264",
                   CodecType.Video,
                   new[]
                   {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "37500000"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoLevel, "41")
                   }),

                new CodecProfile(
                    "wmv2,wmv3,vc1",
                    CodecType.Video,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "25600000"),
                    }),

                new CodecProfile(
                    "wmav2,dca,aac,mp3,dts",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "6")
                    })
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile("avi", DlnaProfileType.Video, "video/x-msvideo"),
                new ResponseProfile("mkv", DlnaProfileType.Video, "video/x-mkv"),
                new ResponseProfile("flac", DlnaProfileType.Audio, "audio/x-flac"),
                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4")
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed),
                new SubtitleProfile("srt", SubtitleDeliveryMethod.External)
                {
                    DidlMode = "CaptionInfoEx"
                }
            };
        }
    }
}
