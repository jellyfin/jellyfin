#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SonyBlurayPlayer2016 : DefaultProfile
    {
        public SonyBlurayPlayer2016()
        {
            Name = "Sony Blu-ray Player 2016";

            Identification = new DeviceIdentification
            {
                ModelNumber = "BDP-2016",

                Headers = new[]
                {
                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Physical-Unit-Info",
                        Value = "BDP-S1700",
                        Match = HeaderMatchType.Substring
                    },
                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Physical-Unit-Info",
                        Value = "BDP-S3700",
                        Match = HeaderMatchType.Substring
                    },
                    new HttpHeaderInfo
                    {
                        Name = "X-AV-Physical-Unit-Info",
                        Value = "BDP-S6700",
                        Match = HeaderMatchType.Substring
                    }
                }
            };

            AddXmlRootAttribute("xmlns:av", "urn:schemas-sony-com:av");

            ModelName = "Windows Media Player Sharing";
            ModelNumber = "3.0";
            Manufacturer = "Microsoft Corporation";

            ProtocolInfo = "http-get:*:video/divx:DLNA.ORG_PN=MATROSKA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_NTSC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMABASE;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMAFULL;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_AAC_MULT5;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mpeg:DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=44100;channels=1:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=44100;channels=2:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=48000;channels=1:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=48000;channels=2:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO_320;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS_320;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/flac:DLNA.ORG_PN=FLAC;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/ogg:DLNA.ORG_PN=OGG;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_MED;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/png:DLNA.ORG_PN=PNG_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/png:DLNA.ORG_PN=PNG_TN;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/gif:DLNA.ORG_PN=GIF_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG1;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_EU_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_NA_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_KO_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_JP_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-msvideo:DLNA.ORG_PN=AVI;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-flv:DLNA.ORG_PN=FLV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-dvr:DLNA.ORG_PN=DVR_MS;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/wtv:DLNA.ORG_PN=WTV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/ogg:DLNA.ORG_PN=OGV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.rn-realvideo:DLNA.ORG_PN=REAL_VIDEO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_BASE;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_FULL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_FULL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_PRO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_PRO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L1_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L2_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L3_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_P2_3GPP_SP_L0B_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_P2_3GPP_SP_L0B_AMR;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_H263_3GPP_P0_L10_AMR;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_H263_MP4_P0_L10_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000";

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
                    Container = "mkv",
                    VideoCodec = "h264",
                    AudioCodec = "ac3,aac,mp3",
                    Type = DlnaProfileType.Video
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
                    Container = "ts,mpegts",
                    VideoCodec = "mpeg1video,mpeg2video,h264",
                    AudioCodec = "ac3,aac,mp3,pcm",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mpeg,mpg",
                    VideoCodec = "mpeg1video,mpeg2video",
                    AudioCodec = "ac3,mp3,mp2,pcm",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp4,m4v",
                    VideoCodec = "mpeg4,h264",
                    AudioCodec = "ac3,aac,pcm,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "avi",
                    VideoCodec = "mpeg4,h264",
                    AudioCodec = "ac3,aac,mp3,pcm",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mkv",
                    VideoCodec = "mpeg4,h264",
                    AudioCodec = "ac3,dca,aac,mp3,pcm,dts",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "m2ts,mts",
                    VideoCodec = "h264,mpeg4,vc1",
                    AudioCodec = "aac,mp3,ac3,dca,dts",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "wmv,asf",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp3,m4a,wma,wav",
                    Type = DlnaProfileType.Audio
                },
                new DirectPlayProfile
                {
                    Container = "jpeg,png,gif",
                    Type = DlnaProfileType.Photo
                }
            };

            CodecProfiles = new[]
            {
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec = "h264",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Width,
                            Value = "1920"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoFramerate,
                            Value = "30",
                            IsRequired = false
                        }
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.VideoAudio,
                    Codec = "ac3",
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.AudioChannels,
                            Value = "6",
                            IsRequired = false
                        }
                    }
                }
            };

            ContainerProfiles = new[]
            {
                new ContainerProfile
                {
                    Type = DlnaProfileType.Photo,

                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Width,
                            Value = "1920"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080"
                        }
                    }
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

            ResponseProfiles = new ResponseProfile[] { };
        }
    }
}
