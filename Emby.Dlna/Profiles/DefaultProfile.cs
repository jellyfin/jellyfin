using MediaBrowser.Model.Dlna;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class DefaultProfile : DeviceProfile
    {
        public DefaultProfile()
        {
            Name = "Generic Device";

            ProtocolInfo = "http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_50_AC3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_50_AC3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_HD_50_AC3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:audio/mpeg:DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:audio/L16;rate=44100;channels=1:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:audio/L16;rate=44100;channels=2:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:audio/L16;rate=48000;channels=1:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:audio/L16;rate=48000;channels=2:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMA_BASE;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMA_FULL;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_MED;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG1;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_NTSC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_EU_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_NA_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_KO_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-msvideo:DLNA.ORG_PN=AVI;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-matroska:DLNA.ORG_PN=MATROSKA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_AAC_MULT5;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_MPEG1_L3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_AC3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_HD_720p_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_HD_1080i_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_HP_HD_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_LPCM;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_ASP_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_SP_L6_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_NDSD;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_AAC_MULT5;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_AAC_MULT5_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_SD_AAC_MULT5_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_AAC_MULT5;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_AAC_MULT5_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_HD_AAC_MULT5_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_50_LPCM_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_BASE;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_FULL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_FULL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_PRO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_PRO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L1_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L2_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L3_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=01500000000000000000000000000000";

            XDlnaDoc = "DMS-1.50";

            Manufacturer = "Emby";
            ModelDescription = "Emby";
            ModelName = "Emby Server";
            ModelNumber = "Emby";
            ModelUrl = "http://emby.media/";
            ManufacturerUrl = "http://emby.media/";

            AlbumArtPn = "JPEG_SM";

            MaxAlbumArtHeight = 480;
            MaxAlbumArtWidth = 480;

            MaxIconWidth = 48;
            MaxIconHeight = 48;

            MaxStreamingBitrate = 20000000;
            MaxStaticBitrate = 20000000;
            MusicStreamingTranscodingBitrate = 192000;

            EnableAlbumArtInDidl = false;

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio
                },

                new TranscodingProfile
                {
                    Container = "ts",
                    Type = DlnaProfileType.Video,
                    AudioCodec = "aac",
                    VideoCodec = "h264"
                },

                new TranscodingProfile
                {
                    Container = "jpeg",
                    Type = DlnaProfileType.Photo
                }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "m4v,ts,mkv,avi,mpg,mpeg,mp4",
                    VideoCodec = "h264",
                    AudioCodec = "aac,mp3,ac3",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mp3,wma,aac,wav",
                    Type = DlnaProfileType.Audio
                }
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.Embed
                }
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile
                {
                    Container = "m4v",
                    Type = DlnaProfileType.Video,
                    MimeType = "video/mp4"
                }
            };
        }

        public void AddXmlRootAttribute(string name, string value)
        {
            var atts = XmlRootAttributes ?? new XmlAttribute[] { };
            var list = atts.ToList();

            list.Add(new XmlAttribute
            {
                Name = name,
                Value = value
            });

            XmlRootAttributes = list.ToArray();
        }
    }
}
