using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class MediaFormatProfileResolver
    {
        public List<MediaFormatProfile> ResolveVideoFormat(string container, string videoCodec, string audioCodec, int? width, int? height, TransportStreamTimestamp timestampType)
        {
            if (StringHelper.EqualsIgnoreCase(container, "asf"))
            {
                MediaFormatProfile? val = ResolveVideoASFFormat(videoCodec, audioCodec, width, height);
                return val.HasValue ? new List<MediaFormatProfile> { val.Value } : new List<MediaFormatProfile>();
            }

            if (StringHelper.EqualsIgnoreCase(container, "mp4"))
            {
                MediaFormatProfile? val = ResolveVideoMP4Format(videoCodec, audioCodec, width, height);
                return val.HasValue ? new List<MediaFormatProfile> { val.Value } : new List<MediaFormatProfile>();
            }

            if (StringHelper.EqualsIgnoreCase(container, "avi"))
                return new List<MediaFormatProfile> { MediaFormatProfile.AVI };

            if (StringHelper.EqualsIgnoreCase(container, "mkv"))
                return new List<MediaFormatProfile> { MediaFormatProfile.MATROSKA };

            if (StringHelper.EqualsIgnoreCase(container, "mpeg2ps") ||
                StringHelper.EqualsIgnoreCase(container, "ts"))

                return new List<MediaFormatProfile> { MediaFormatProfile.MPEG_PS_NTSC, MediaFormatProfile.MPEG_PS_PAL };

            if (StringHelper.EqualsIgnoreCase(container, "mpeg1video"))
                return new List<MediaFormatProfile> { MediaFormatProfile.MPEG1 };

            if (StringHelper.EqualsIgnoreCase(container, "mpeg2ts") ||
                StringHelper.EqualsIgnoreCase(container, "mpegts") ||
                StringHelper.EqualsIgnoreCase(container, "m2ts"))
            {

                return ResolveVideoMPEG2TSFormat(videoCodec, audioCodec, width, height, timestampType);
            }

            if (StringHelper.EqualsIgnoreCase(container, "flv"))
                return new List<MediaFormatProfile> { MediaFormatProfile.FLV };

            if (StringHelper.EqualsIgnoreCase(container, "wtv"))
                return new List<MediaFormatProfile> { MediaFormatProfile.WTV };

            if (StringHelper.EqualsIgnoreCase(container, "3gp"))
            {
                MediaFormatProfile? val = ResolveVideo3GPFormat(videoCodec, audioCodec);
                return val.HasValue ? new List<MediaFormatProfile> { val.Value } : new List<MediaFormatProfile>();
            }

            if (StringHelper.EqualsIgnoreCase(container, "ogv") || StringHelper.EqualsIgnoreCase(container, "ogg"))
                return new List<MediaFormatProfile> { MediaFormatProfile.OGV };

            return new List<MediaFormatProfile>();
        }

        private List<MediaFormatProfile> ResolveVideoMPEG2TSFormat(string videoCodec, string audioCodec, int? width, int? height, TransportStreamTimestamp timestampType)
        {
            string suffix = "";

            switch (timestampType)
            {
                case TransportStreamTimestamp.None:
                    suffix = "_ISO";
                    break;
                case TransportStreamTimestamp.Valid:
                    suffix = "_T";
                    break;
            }

            string resolution = "S";
            if ((width.HasValue && width.Value > 720) || (height.HasValue && height.Value > 576))
            {
                resolution = "H";
            }

            if (StringHelper.EqualsIgnoreCase(videoCodec, "mpeg2video"))
            {
                List<MediaFormatProfile> list = new List<MediaFormatProfile>();

                list.Add(ValueOf("MPEG_TS_SD_NA" + suffix));
                list.Add(ValueOf("MPEG_TS_SD_EU" + suffix));
                list.Add(ValueOf("MPEG_TS_SD_KO" + suffix));

                if ((timestampType == TransportStreamTimestamp.Valid) && StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                {
                    list.Add(MediaFormatProfile.MPEG_TS_JP_T);
                }
                return list;
            }
            if (StringHelper.EqualsIgnoreCase(videoCodec, "h264"))
            {
                if (StringHelper.EqualsIgnoreCase(audioCodec, "lpcm"))
                    return new List<MediaFormatProfile> { MediaFormatProfile.AVC_TS_HD_50_LPCM_T };

                if (StringHelper.EqualsIgnoreCase(audioCodec, "dts"))
                {
                    if (timestampType == TransportStreamTimestamp.None)
                    {
                        return new List<MediaFormatProfile> { MediaFormatProfile.AVC_TS_HD_DTS_ISO };
                    }
                    return new List<MediaFormatProfile> { MediaFormatProfile.AVC_TS_HD_DTS_T };
                }

                if (StringHelper.EqualsIgnoreCase(audioCodec, "mp2"))
                {
                    if (timestampType == TransportStreamTimestamp.None)
                    {
                        return new List<MediaFormatProfile> { ValueOf(string.Format("AVC_TS_HP_{0}D_MPEG1_L2_ISO", resolution)) };
                    }

                    return new List<MediaFormatProfile> { ValueOf(string.Format("AVC_TS_HP_{0}D_MPEG1_L2_T", resolution)) };
                }

                if (StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                    return new List<MediaFormatProfile> { ValueOf(string.Format("AVC_TS_MP_{0}D_AAC_MULT5{1}", resolution, suffix)) };

                if (StringHelper.EqualsIgnoreCase(audioCodec, "mp3"))
                    return new List<MediaFormatProfile> { ValueOf(string.Format("AVC_TS_MP_{0}D_MPEG1_L3{1}", resolution, suffix)) };

                if (string.IsNullOrEmpty(audioCodec) ||
                    StringHelper.EqualsIgnoreCase(audioCodec, "ac3"))
                    return new List<MediaFormatProfile> { ValueOf(string.Format("AVC_TS_MP_{0}D_AC3{1}", resolution, suffix)) };
            }
            else if (StringHelper.EqualsIgnoreCase(videoCodec, "vc1"))
            {
                if (string.IsNullOrEmpty(audioCodec) || StringHelper.EqualsIgnoreCase(audioCodec, "ac3"))
                {
                    if ((width.HasValue && width.Value > 720) || (height.HasValue && height.Value > 576))
                    {
                        return new List<MediaFormatProfile> { MediaFormatProfile.VC1_TS_AP_L2_AC3_ISO };
                    }
                    return new List<MediaFormatProfile> { MediaFormatProfile.VC1_TS_AP_L1_AC3_ISO };
                }
                if (StringHelper.EqualsIgnoreCase(audioCodec, "dts"))
                {
                    suffix = StringHelper.EqualsIgnoreCase(suffix, "_ISO") ? suffix : "_T";

                    return new List<MediaFormatProfile> { ValueOf(string.Format("VC1_TS_HD_DTS{0}", suffix)) };
                }

            }
            else if (StringHelper.EqualsIgnoreCase(videoCodec, "mpeg4") || StringHelper.EqualsIgnoreCase(videoCodec, "msmpeg4"))
            {
                if (StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                    return new List<MediaFormatProfile> { ValueOf(string.Format("MPEG4_P2_TS_ASP_AAC{0}", suffix)) };
                if (StringHelper.EqualsIgnoreCase(audioCodec, "mp3"))
                    return new List<MediaFormatProfile> { ValueOf(string.Format("MPEG4_P2_TS_ASP_MPEG1_L3{0}", suffix)) };
                if (StringHelper.EqualsIgnoreCase(audioCodec, "mp2"))
                    return new List<MediaFormatProfile> { ValueOf(string.Format("MPEG4_P2_TS_ASP_MPEG2_L2{0}", suffix)) };
                if (StringHelper.EqualsIgnoreCase(audioCodec, "ac3"))
                    return new List<MediaFormatProfile> { ValueOf(string.Format("MPEG4_P2_TS_ASP_AC3{0}", suffix)) };
            }

            return new List<MediaFormatProfile>();
        }

        private MediaFormatProfile ValueOf(string value)
        {
            return (MediaFormatProfile)Enum.Parse(typeof(MediaFormatProfile), value, true);
        }

        private MediaFormatProfile? ResolveVideoMP4Format(string videoCodec, string audioCodec, int? width, int? height)
        {
            if (StringHelper.EqualsIgnoreCase(videoCodec, "h264"))
            {
                if (StringHelper.EqualsIgnoreCase(audioCodec, "lpcm"))
                    return MediaFormatProfile.AVC_MP4_LPCM;
                if (string.IsNullOrEmpty(audioCodec) ||
                    StringHelper.EqualsIgnoreCase(audioCodec, "ac3"))
                {
                    return MediaFormatProfile.AVC_MP4_MP_SD_AC3;
                }
                if (StringHelper.EqualsIgnoreCase(audioCodec, "mp3"))
                {
                    return MediaFormatProfile.AVC_MP4_MP_SD_MPEG1_L3;
                }
                if (width.HasValue && height.HasValue)
                {
                    if ((width.Value <= 720) && (height.Value <= 576))
                    {
                        if (StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                            return MediaFormatProfile.AVC_MP4_MP_SD_AAC_MULT5;
                    }
                    else if ((width.Value <= 1280) && (height.Value <= 720))
                    {
                        if (StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                            return MediaFormatProfile.AVC_MP4_MP_HD_720p_AAC;
                    }
                    else if ((width.Value <= 1920) && (height.Value <= 1080))
                    {
                        if (StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                        {
                            return MediaFormatProfile.AVC_MP4_MP_HD_1080i_AAC;
                        }
                    }
                }
            }
            else if (StringHelper.EqualsIgnoreCase(videoCodec, "mpeg4") ||
                StringHelper.EqualsIgnoreCase(videoCodec, "msmpeg4"))
            {
                if (width.HasValue && height.HasValue && width.Value <= 720 && height.Value <= 576)
                {
                    if (string.IsNullOrEmpty(audioCodec) || StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                        return MediaFormatProfile.MPEG4_P2_MP4_ASP_AAC;
                    if (StringHelper.EqualsIgnoreCase(audioCodec, "ac3") || StringHelper.EqualsIgnoreCase(audioCodec, "mp3"))
                    {
                        return MediaFormatProfile.MPEG4_P2_MP4_NDSD;
                    }
                }
                else if (string.IsNullOrEmpty(audioCodec) || StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                {
                    return MediaFormatProfile.MPEG4_P2_MP4_SP_L6_AAC;
                }
            }
            else if (StringHelper.EqualsIgnoreCase(videoCodec, "h263") && StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
            {
                return MediaFormatProfile.MPEG4_H263_MP4_P0_L10_AAC;
            }

            return null;
        }

        private MediaFormatProfile? ResolveVideo3GPFormat(string videoCodec, string audioCodec)
        {
            if (StringHelper.EqualsIgnoreCase(videoCodec, "h264"))
            {
                if (string.IsNullOrEmpty(audioCodec) || StringHelper.EqualsIgnoreCase(audioCodec, "aac"))
                    return MediaFormatProfile.AVC_3GPP_BL_QCIF15_AAC;
            }
            else if (StringHelper.EqualsIgnoreCase(videoCodec, "mpeg4") ||
                StringHelper.EqualsIgnoreCase(videoCodec, "msmpeg4"))
            {
                if (string.IsNullOrEmpty(audioCodec) || StringHelper.EqualsIgnoreCase(audioCodec, "wma"))
                    return MediaFormatProfile.MPEG4_P2_3GPP_SP_L0B_AAC;
                if (StringHelper.EqualsIgnoreCase(audioCodec, "amrnb"))
                    return MediaFormatProfile.MPEG4_P2_3GPP_SP_L0B_AMR;
            }
            else if (StringHelper.EqualsIgnoreCase(videoCodec, "h263") && StringHelper.EqualsIgnoreCase(audioCodec, "amrnb"))
            {
                return MediaFormatProfile.MPEG4_H263_3GPP_P0_L10_AMR;
            }

            return null;
        }

        private MediaFormatProfile? ResolveVideoASFFormat(string videoCodec, string audioCodec, int? width, int? height)
        {
            if (StringHelper.EqualsIgnoreCase(videoCodec, "wmv") &&
                (string.IsNullOrEmpty(audioCodec) || StringHelper.EqualsIgnoreCase(audioCodec, "wma") || StringHelper.EqualsIgnoreCase(videoCodec, "wmapro")))
            {

                if (width.HasValue && height.HasValue)
                {
                    if ((width.Value <= 720) && (height.Value <= 576))
                    {
                        if (string.IsNullOrEmpty(audioCodec) || StringHelper.EqualsIgnoreCase(audioCodec, "wma"))
                        {
                            return MediaFormatProfile.WMVMED_FULL;
                        }
                        return MediaFormatProfile.WMVMED_PRO;
                    }
                }

                if (string.IsNullOrEmpty(audioCodec) || StringHelper.EqualsIgnoreCase(audioCodec, "wma"))
                {
                    return MediaFormatProfile.WMVHIGH_FULL;
                }
                return MediaFormatProfile.WMVHIGH_PRO;
            }

            if (StringHelper.EqualsIgnoreCase(videoCodec, "vc1"))
            {
                if (width.HasValue && height.HasValue)
                {
                    if ((width.Value <= 720) && (height.Value <= 576))
                        return MediaFormatProfile.VC1_ASF_AP_L1_WMA;
                    if ((width.Value <= 1280) && (height.Value <= 720))
                        return MediaFormatProfile.VC1_ASF_AP_L2_WMA;
                    if ((width.Value <= 1920) && (height.Value <= 1080))
                        return MediaFormatProfile.VC1_ASF_AP_L3_WMA;
                }
            }
            else if (StringHelper.EqualsIgnoreCase(videoCodec, "mpeg2video"))
            {
                return MediaFormatProfile.DVR_MS;
            }

            return null;
        }

        public MediaFormatProfile? ResolveAudioFormat(string container, int? bitrate, int? frequency, int? channels)
        {
            if (StringHelper.EqualsIgnoreCase(container, "asf"))
                return ResolveAudioASFFormat(bitrate);

            if (StringHelper.EqualsIgnoreCase(container, "mp3"))
                return MediaFormatProfile.MP3;

            if (StringHelper.EqualsIgnoreCase(container, "lpcm"))
                return ResolveAudioLPCMFormat(frequency, channels);

            if (StringHelper.EqualsIgnoreCase(container, "mp4") ||
                StringHelper.EqualsIgnoreCase(container, "aac"))
                return ResolveAudioMP4Format(bitrate);

            if (StringHelper.EqualsIgnoreCase(container, "adts"))
                return ResolveAudioADTSFormat(bitrate);

            if (StringHelper.EqualsIgnoreCase(container, "flac"))
                return MediaFormatProfile.FLAC;

            if (StringHelper.EqualsIgnoreCase(container, "oga") ||
                StringHelper.EqualsIgnoreCase(container, "ogg"))
                return MediaFormatProfile.OGG;

            return null;
        }

        private MediaFormatProfile ResolveAudioASFFormat(int? bitrate)
        {
            if (bitrate.HasValue && bitrate.Value <= 193)
            {
                return MediaFormatProfile.WMA_BASE;
            }
            return MediaFormatProfile.WMA_FULL;
        }

        private MediaFormatProfile? ResolveAudioLPCMFormat(int? frequency, int? channels)
        {
            if (frequency.HasValue && channels.HasValue)
            {
                if (frequency.Value == 44100 && channels.Value == 1)
                {
                    return MediaFormatProfile.LPCM16_44_MONO;
                }
                if (frequency.Value == 44100 && channels.Value == 2)
                {
                    return MediaFormatProfile.LPCM16_44_STEREO;
                }
                if (frequency.Value == 48000 && channels.Value == 1)
                {
                    return MediaFormatProfile.LPCM16_48_MONO;
                }
                if (frequency.Value == 48000 && channels.Value == 2)
                {
                    return MediaFormatProfile.LPCM16_48_STEREO;
                }

                return null;
            }

            return MediaFormatProfile.LPCM16_48_STEREO;
        }

        private MediaFormatProfile ResolveAudioMP4Format(int? bitrate)
        {
            if (bitrate.HasValue && bitrate.Value <= 320)
            {
                return MediaFormatProfile.AAC_ISO_320;
            }
            return MediaFormatProfile.AAC_ISO;
        }

        private MediaFormatProfile ResolveAudioADTSFormat(int? bitrate)
        {
            if (bitrate.HasValue && bitrate.Value <= 320)
            {
                return MediaFormatProfile.AAC_ADTS_320;
            }
            return MediaFormatProfile.AAC_ADTS;
        }

        public MediaFormatProfile? ResolveImageFormat(string container, int? width, int? height)
        {
            if (StringHelper.EqualsIgnoreCase(container, "jpeg") ||
                StringHelper.EqualsIgnoreCase(container, "jpg"))
                return ResolveImageJPGFormat(width, height);

            if (StringHelper.EqualsIgnoreCase(container, "png"))
                return ResolveImagePNGFormat(width, height);

            if (StringHelper.EqualsIgnoreCase(container, "gif"))
                return MediaFormatProfile.GIF_LRG;

            if (StringHelper.EqualsIgnoreCase(container, "raw"))
                return MediaFormatProfile.RAW;

            return null;
        }

        private MediaFormatProfile ResolveImageJPGFormat(int? width, int? height)
        {
            if (width.HasValue && height.HasValue)
            {
                if ((width.Value <= 160) && (height.Value <= 160))
                    return MediaFormatProfile.JPEG_TN;

                if ((width.Value <= 640) && (height.Value <= 480))
                    return MediaFormatProfile.JPEG_SM;

                if ((width.Value <= 1024) && (height.Value <= 768))
                {
                    return MediaFormatProfile.JPEG_MED;
                }

                return MediaFormatProfile.JPEG_LRG;
            }

            return MediaFormatProfile.JPEG_SM;
        }

        private MediaFormatProfile ResolveImagePNGFormat(int? width, int? height)
        {
            if (width.HasValue && height.HasValue)
            {
                if ((width.Value <= 160) && (height.Value <= 160))
                    return MediaFormatProfile.PNG_TN;
            }

            return MediaFormatProfile.PNG_LRG;
        }
    }
}
