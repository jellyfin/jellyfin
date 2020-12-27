#pragma warning disable SA1118 // Parameter should not span multiple lines

using MediaBrowser.Model.Dlna;

namespace Jellyfin.DlnaProfiles.Profiles
{
    /// <summary>
    /// Defines the <see cref="SonyBlurayPlayerProfile" />.
    /// </summary>
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SonyBlurayPlayerProfile : DefaultProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SonyBlurayPlayerProfile"/> class.
        /// </summary>
        public SonyBlurayPlayerProfile()
        {
            Name = "Sony Blu-ray Player";

            Identification = new DeviceIdentification(
                @"Blu-ray Disc Player",
                new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Client-Info",
                        Value = @"(Blu-ray Disc Player|Home Theater System|Home Theatre System|Media Player)",
                        Match = HeaderMatchType.Regex
                    },

                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Physical-Unit-Info",
                        Value = @"(Blu-ray Disc Player|Home Theater System|Home Theatre System|Media Player)",
                        Match = HeaderMatchType.Regex
                    }
                })
            {
                Manufacturer = "Sony"
            };

            AddXmlRootAttribute("xmlns:av", "urn:schemas-sony-com:av");

            ModelName = "Windows Media Player Sharing";
            ModelNumber = "3.0";
            Manufacturer = "Microsoft Corporation";

            ProtocolInfo = "http-get:*:video/divx:DLNA.ORG_PN=MATROSKA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_NTSC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMABASE;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMAFULL;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_AAC_MULT5;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mpeg:DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=44100;channels=1:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=44100;channels=2:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=48000;channels=1:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=48000;channels=2:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO_320;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS_320;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/flac:DLNA.ORG_PN=FLAC;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/ogg:DLNA.ORG_PN=OGG;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_MED;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/png:DLNA.ORG_PN=PNG_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/png:DLNA.ORG_PN=PNG_TN;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/gif:DLNA.ORG_PN=GIF_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG1;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_EU_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_NA_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_KO_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_JP_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-msvideo:DLNA.ORG_PN=AVI;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-flv:DLNA.ORG_PN=FLV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-dvr:DLNA.ORG_PN=DVR_MS;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/wtv:DLNA.ORG_PN=WTV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/ogg:DLNA.ORG_PN=OGV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.rn-realvideo:DLNA.ORG_PN=REAL_VIDEO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_BASE;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_FULL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_FULL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_PRO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_PRO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L1_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L2_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L3_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_P2_3GPP_SP_L0B_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_P2_3GPP_SP_L0B_AMR;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_H263_3GPP_P0_L10_AMR;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_H263_MP4_P0_L10_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000";

            TranscodingProfiles = new[]
            {
                new TranscodingProfile("mp3", "mp3"),
                new TranscodingProfile("ts", "mpeg2video", "ac3"),
                new TranscodingProfile("jpeg")
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile("ts,mpegts", "mpeg1video,mpeg2video,h264", "ac3,aac,mp3,pcm"),
                new DirectPlayProfile("mpeg", "mpeg1video,mpeg2video", "ac3,mp3,pcm"),
                new DirectPlayProfile("avi,mp4,m4v", "mpeg4,h264", "ac3,aac,mp3,pcm"),
                new DirectPlayProfile("mp3", "mp3"),
                new DirectPlayProfile("asf", "wmav2,wmapro,wmavoice"),
                new DirectPlayProfile("jpeg")
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
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoLevel, "41", false),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "15360000", false)
                    }),

                new CodecProfile(
                    "ac3",
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
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2", false)
                    })
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

            ResponseProfiles = new[]
            {
                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, "video/vnd.dlna.mpeg-tts")
                {
                    VideoCodec = "h264,mpeg4,vc1",
                    AudioCodec = "ac3,aac,mp3",
                    OrgPn = "MPEG_TS_SD_EU,MPEG_TS_SD_NA,MPEG_TS_SD_KO"
                },

                new ResponseProfile("avi", DlnaProfileType.Video, "video/mpeg"),
                new ResponseProfile("mkv", DlnaProfileType.Video, "video/vnd.dlna.mpeg-tts"),
                new ResponseProfile("ts,mpegts", DlnaProfileType.Video, "video/vnd.dlna.mpeg-tts"),
                new ResponseProfile("mp4", DlnaProfileType.Video, "video/mpeg"),
                new ResponseProfile("m4v", DlnaProfileType.Video, "video/mpeg"),
                new ResponseProfile("mpeg", DlnaProfileType.Video, "video/mpeg"),
                new ResponseProfile("mp3", DlnaProfileType.Audio, "audio/mpeg")
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile("srt", SubtitleDeliveryMethod.Embed)
            };
        }
    }
}
