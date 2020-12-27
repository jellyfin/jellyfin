using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="SonyBravia2014Profile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SonyBravia2014Profile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SonyBravia2014Profile"/> class.
        /// </summary>
        public SonyBravia2014Profile()
        {
            Name = "Sony Bravia (2014)";

            Identification = new DeviceIdentification(
                @"(KDL-\d{2}W[5-9]\d{2}B|KDL-\d{2}R480|XBR-\d{2}X[89]\d{2}B|KD-\d{2}[SX][89]\d{3}B).*",
                new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Client-Info",
                        Value = @".*(KDL-\d{2}W[5-9]\d{2}B|KDL-\d{2}R480|XBR-\d{2}X[89]\d{2}B|KD-\d{2}[SX][89]\d{3}B).*",
                        Match = HeaderMatchType.Regex
                    }
                })
            {
                Manufacturer = "Sony"
            };

            AddXmlRootAttribute("xmlns:av", "urn:schemas-sony-com:av");

            AlbumArtPn = "JPEG_TN";

            ModelName = "Windows Media Player Sharing";
            ModelNumber = "3.0";
            ModelUrl = "http://www.microsoft.com/";
            Manufacturer = "Microsoft Corporation";
            ManufacturerUrl = "http://www.microsoft.com/";
            SonyAggregationFlags = "10";
            EnableSingleAlbumArtLimit = true;
            EnableAlbumArtInDidl = true;

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", null),
                new TranscodingProfile("ts", "h264", "ac3")
                {
                    EnableMpegtsM2TsMode = true
                },
                new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("ts,mpegts", "h264", "ac3,eac3,aac,mp3"),
                new DirectPlayProfile("ts,mpegts", "mpeg2video", "mp3,mp2"),
                new DirectPlayProfile("mp4,m4v", "h264,mpeg4", "ac3,eac3,aac,mp3,mp2"),
                new DirectPlayProfile("mov", "h264,mpeg4,mjpeg", "ac3,eac3,aac,mp3,mp2"),
                new DirectPlayProfile("mkv", "h264,mpeg4,vp8", "ac3,eac3,aac,mp3,mp2,pcm,vorbis"),
                new DirectPlayProfile("avi", "mpeg4", "ac3,eac3,mp3"),
                new DirectPlayProfile("avi", "mjpeg", "pcm"),
                new DirectPlayProfile("mpeg", "mpeg2video,mpeg1video", "mp3,mp2"),
                new DirectPlayProfile("asf", "wmv2,wmv3,vc1", "wmav2,wmapro,wmavoice"),
                new DirectPlayProfile("mp3", "mp3"),
                new DirectPlayProfile("mp4", "aac"),
                new DirectPlayProfile("wav", "pcm"),
                new DirectPlayProfile("asf", "wmav2,wmapro,wmavoice"),
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

            ResponseProfiles = new[]
            {
                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, "video/vnd.dlna.mpeg-tts")
                {
                    VideoCodec = "h264",
                    AudioCodec = "ac3,aac,mp3",
                    OrgPn = "AVC_TS_HD_24_AC3_T,AVC_TS_HD_50_AC3_T,AVC_TS_HD_60_AC3_T,AVC_TS_HD_EU_T",
                    Conditions = new[]
                    {
                        new ProfileCondition(ProfileConditionType.Equals, ProfileConditionValue.PacketLength, "192"),
                        new ProfileCondition(ProfileConditionType.Equals, ProfileConditionValue.VideoTimestamp, "Valid")
                    }
                },

                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, "video/mpeg")
                {
                    VideoCodec = "h264",
                    AudioCodec = "ac3,aac,mp3",
                    OrgPn = "AVC_TS_HD_24_AC3_ISO,AVC_TS_HD_50_AC3_ISO,AVC_TS_HD_60_AC3_ISO,AVC_TS_HD_EU_ISO",
                    Conditions = new[]
                    {
                        new ProfileCondition(ProfileConditionType.Equals, ProfileConditionValue.PacketLength, "188")
                    }
                },

                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, "video/vnd.dlna.mpeg-tts")
                {
                    VideoCodec = "h264",
                    AudioCodec = "ac3,aac,mp3",
                    OrgPn = "AVC_TS_HD_24_AC3,AVC_TS_HD_50_AC3,AVC_TS_HD_60_AC3,AVC_TS_HD_EU"
                },

                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, "video/vnd.dlna.mpeg-tts")
                {
                    VideoCodec = "mpeg2video",
                    OrgPn = "MPEG_TS_SD_EU,MPEG_TS_SD_NA,MPEG_TS_SD_KO"
                },

                new ResponseProfile("mpeg", DlnaProfileType.Video, "video/mpeg")
                {
                    VideoCodec = "mpeg1video,mpeg2video",
                    OrgPn = "MPEG_PS_NTSC,MPEG_PS_PAL"
                },

                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mp4")
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
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoFramerate, "30")
                    }),

                new CodecProfile(
                    "mp3,mp2",
                    CodecType.VideoAudio,
                    new[]
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2")
                    })
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed)
            };
        }
    }
}
