using System;

namespace MediaBrowser.Model.Dlna
{
    public class MediaFormatProfileResolver
    {
        public MediaFormatProfile ResolveVideoFormat(string container, string videoCodec, string audioCodec, int? width, int? height, int? bitrate, TransportStreamTimestamp timestampType)
        {
            if (string.Equals(container, "asf", StringComparison.OrdinalIgnoreCase))
                return ResolveVideoASFFormat(videoCodec, audioCodec, width, height, bitrate);
            if (string.Equals(container, "mp4", StringComparison.OrdinalIgnoreCase))
                return ResolveVideoMP4Format(videoCodec, audioCodec, width, height, bitrate);
            if (string.Equals(container, "avi", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.AVI;
            if (string.Equals(container, "mkv", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.MATROSKA;
            if (string.Equals(container, "mpeg2ps", StringComparison.OrdinalIgnoreCase) || string.Equals(container, "ts", StringComparison.OrdinalIgnoreCase))
                // MediaFormatProfile.MPEG_PS_PAL, MediaFormatProfile.MPEG_PS_NTSC
                return MediaFormatProfile.MPEG_PS_NTSC;
            if (string.Equals(container, "mpeg1video", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.MPEG1;
            if (string.Equals(container, "mpeg2ts", StringComparison.OrdinalIgnoreCase) || string.Equals(container, "mpegts", StringComparison.OrdinalIgnoreCase) || string.Equals(container, "m2ts", StringComparison.OrdinalIgnoreCase))
                return ResolveVideoMPEG2TSFormat(videoCodec, audioCodec, width, height, bitrate, timestampType);
            if (string.Equals(container, "flv", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.FLV;
            if (string.Equals(container, "wtv", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.WTV;
            if (string.Equals(container, "3gp", StringComparison.OrdinalIgnoreCase))
                return ResolveVideo3GPFormat(videoCodec, audioCodec, width, height, bitrate);
            if (string.Equals(container, "ogv", StringComparison.OrdinalIgnoreCase) || string.Equals(container, "ogg", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.OGV;

            throw new ArgumentException("Unsupported container: " + container);
        }

        private MediaFormatProfile ResolveVideoMPEG2TSFormat(string videoCodec, string audioCodec, int? width, int? height, int? bitrate, TransportStreamTimestamp timestampType)
        {
            //  String suffix = "";
            //  if (isNoTimestamp(timestampType))
            //    suffix = "_ISO";
            //  else if (timestampType == TransportStreamTimestamp.VALID) {
            //    suffix = "_T";
            //  }

            //  String resolution = "S";
            //  if ((width.intValue() > 720) || (height.intValue() > 576)) {
            //    resolution = "H";
            //  }

            //  if (videoCodec == VideoCodec.MPEG2)
            //  {
            //    List!(MediaFormatProfile) profiles = Arrays.asList(cast(MediaFormatProfile[])[ MediaFormatProfile.valueOf("MPEG_TS_SD_EU" + suffix), MediaFormatProfile.valueOf("MPEG_TS_SD_NA" + suffix), MediaFormatProfile.valueOf("MPEG_TS_SD_KO" + suffix) ]);

            //    if ((timestampType == TransportStreamTimestamp.VALID) && (audioCodec == AudioCodec.AAC)) {
            //      profiles.add(MediaFormatProfile.MPEG_TS_JP_T);
            //    }
            //    return profiles;
            //  }if (videoCodec == VideoCodec.H264)
            //  {
            //    if (audioCodec == AudioCodec.LPCM)
            //      return Collections.singletonList(MediaFormatProfile.AVC_TS_HD_50_LPCM_T);
            //    if (audioCodec == AudioCodec.DTS) {
            //      if (isNoTimestamp(timestampType)) {
            //        return Collections.singletonList(MediaFormatProfile.AVC_TS_HD_DTS_ISO);
            //      }
            //      return Collections.singletonList(MediaFormatProfile.AVC_TS_HD_DTS_T);
            //    }
            //    if (audioCodec == AudioCodec.MP2) {
            //      if (isNoTimestamp(timestampType)) {
            //        return Collections.singletonList(MediaFormatProfile.valueOf(String.format("AVC_TS_HP_%sD_MPEG1_L2_ISO", cast(Object[])[ resolution ])));
            //      }
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("AVC_TS_HP_%sD_MPEG1_L2_T", cast(Object[])[ resolution ])));
            //    }

            //    if (audioCodec == AudioCodec.AAC)
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("AVC_TS_MP_%sD_AAC_MULT5%s", cast(Object[])[ resolution, suffix ])));
            //    if (audioCodec == AudioCodec.MP3)
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("AVC_TS_MP_%sD_MPEG1_L3%s", cast(Object[])[ resolution, suffix ])));
            //    if ((audioCodec is null) || (audioCodec == AudioCodec.AC3)) {
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("AVC_TS_MP_%sD_AC3%s", cast(Object[])[ resolution, suffix ])));
            //    }
            //  }
            //  else if (videoCodec == VideoCodec.VC1) {
            //    if ((audioCodec is null) || (audioCodec == AudioCodec.AC3))
            //    {
            //      if ((width.intValue() > 720) || (height.intValue() > 576)) {
            //        return Collections.singletonList(MediaFormatProfile.VC1_TS_AP_L2_AC3_ISO);
            //      }
            //      return Collections.singletonList(MediaFormatProfile.VC1_TS_AP_L1_AC3_ISO);
            //    }
            //    if (audioCodec == AudioCodec.DTS) {
            //      suffix = suffix.equals("_ISO") ? suffix : "_T";
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("VC1_TS_HD_DTS%s", cast(Object[])[ suffix ])));
            //    }
            //  } else if ((videoCodec == VideoCodec.MPEG4) || (videoCodec == VideoCodec.MSMPEG4)) {
            //    if (audioCodec == AudioCodec.AAC)
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("MPEG4_P2_TS_ASP_AAC%s", cast(Object[])[ suffix ])));
            //    if (audioCodec == AudioCodec.MP3)
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("MPEG4_P2_TS_ASP_MPEG1_L3%s", cast(Object[])[ suffix ])));
            //    if (audioCodec == AudioCodec.MP2)
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("MPEG4_P2_TS_ASP_MPEG2_L2%s", cast(Object[])[ suffix ])));
            //    if ((audioCodec is null) || (audioCodec == AudioCodec.AC3)) {
            //      return Collections.singletonList(MediaFormatProfile.valueOf(String.format("MPEG4_P2_TS_ASP_AC3%s", cast(Object[])[ suffix ])));
            //    }
            //  }

            throw new ArgumentException("Mpeg video file does not match any supported DLNA profile");
        }

        private MediaFormatProfile ResolveVideoMP4Format(string videoCodec, string audioCodec, int? width, int? height, int? bitrate)
        {
            if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(audioCodec, "lpcm", StringComparison.OrdinalIgnoreCase))
                    return MediaFormatProfile.AVC_MP4_LPCM;
                if (string.IsNullOrEmpty(audioCodec) ||
                    string.Equals(audioCodec, "ac3", StringComparison.OrdinalIgnoreCase))
                {
                    return MediaFormatProfile.AVC_MP4_MP_SD_AC3;
                }
                if (string.Equals(audioCodec, "mp3", StringComparison.OrdinalIgnoreCase))
                {
                    return MediaFormatProfile.AVC_MP4_MP_SD_MPEG1_L3;
                }
                if (width.HasValue && height.HasValue)
                {
                    if ((width.Value <= 720) && (height.Value <= 576))
                    {
                        if (string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase))
                            return MediaFormatProfile.AVC_MP4_MP_SD_AAC_MULT5;
                    }
                    else if ((width.Value <= 1280) && (height.Value <= 720))
                    {
                        if (string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase))
                            return MediaFormatProfile.AVC_MP4_MP_HD_720p_AAC;
                    }
                    else if ((width.Value <= 1920) && (height.Value <= 1080))
                    {
                        if (string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase))
                        {
                            return MediaFormatProfile.AVC_MP4_MP_HD_1080i_AAC;
                        }
                    }
                }
            }
            else if (string.Equals(videoCodec, "mpeg4", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
            {
                if (width.HasValue && height.HasValue && width.Value <= 720 && height.Value <= 576)
                {
                    if (string.IsNullOrEmpty(audioCodec) || string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase))
                        return MediaFormatProfile.MPEG4_P2_MP4_ASP_AAC;
                    if (string.Equals(audioCodec, "ac3", StringComparison.OrdinalIgnoreCase) || string.Equals(audioCodec, "mp3", StringComparison.OrdinalIgnoreCase))
                    {
                        return MediaFormatProfile.MPEG4_P2_MP4_NDSD;
                    }
                }
                else if (string.IsNullOrEmpty(audioCodec) || string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase))
                {
                    return MediaFormatProfile.MPEG4_P2_MP4_SP_L6_AAC;
                }
            }
            else if (string.Equals(videoCodec, "h263", StringComparison.OrdinalIgnoreCase) && string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase))
            {
                return MediaFormatProfile.MPEG4_H263_MP4_P0_L10_AAC;
            }

            throw new ArgumentException("MP4 video file does not match any supported DLNA profile");
        }

        private MediaFormatProfile ResolveVideo3GPFormat(string videoCodec, string audioCodec, int? width, int? height, int? bitrate)
        {
            if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(audioCodec) || string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase))
                    return MediaFormatProfile.AVC_3GPP_BL_QCIF15_AAC;
            }
            else if (string.Equals(videoCodec, "mpeg4", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(audioCodec) || string.Equals(audioCodec, "wma", StringComparison.OrdinalIgnoreCase))
                    return MediaFormatProfile.MPEG4_P2_3GPP_SP_L0B_AAC;
                if (string.Equals(audioCodec, "amrnb", StringComparison.OrdinalIgnoreCase))
                    return MediaFormatProfile.MPEG4_P2_3GPP_SP_L0B_AMR;
            }
            else if (string.Equals(videoCodec, "h263", StringComparison.OrdinalIgnoreCase) && string.Equals(audioCodec, "amrnb", StringComparison.OrdinalIgnoreCase))
            {
                return MediaFormatProfile.MPEG4_H263_3GPP_P0_L10_AMR;
            }

            throw new ArgumentException("3GP video file does not match any supported DLNA profile");
        }
        private MediaFormatProfile ResolveVideoASFFormat(string videoCodec, string audioCodec, int? width, int? height, int? bitrate)
        {
            if (string.Equals(videoCodec, "wmv", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(audioCodec) || string.Equals(audioCodec, "wma", StringComparison.OrdinalIgnoreCase) || string.Equals(videoCodec, "wmapro", StringComparison.OrdinalIgnoreCase)))
            {

                if (width.HasValue && height.HasValue)
                {
                    if ((width.Value <= 720) && (height.Value <= 576))
                    {
                        if (string.IsNullOrEmpty(audioCodec) || string.Equals(audioCodec, "wma", StringComparison.OrdinalIgnoreCase))
                        {
                            return MediaFormatProfile.WMVMED_FULL;
                        }
                        return MediaFormatProfile.WMVMED_PRO;
                    }
                }

                if (string.IsNullOrEmpty(audioCodec) || string.Equals(audioCodec, "wma", StringComparison.OrdinalIgnoreCase))
                {
                    return MediaFormatProfile.WMVHIGH_FULL;
                }
                return MediaFormatProfile.WMVHIGH_PRO;
            }

            if (string.Equals(videoCodec, "vc1", StringComparison.OrdinalIgnoreCase))
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
            else if (string.Equals(videoCodec, "mpeg2video", StringComparison.OrdinalIgnoreCase))
            {
                return MediaFormatProfile.DVR_MS;
            }

            throw new ArgumentException("ASF video file does not match any supported DLNA profile");
        }

        public MediaFormatProfile ResolveAudioFormat(string container, int? bitrate, int? frequency, int? channels)
        {
            if (string.Equals(container, "asf", StringComparison.OrdinalIgnoreCase))
                return ResolveAudioASFFormat(bitrate, frequency, channels);
            if (string.Equals(container, "mp3", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.MP3;
            if (string.Equals(container, "lpcm", StringComparison.OrdinalIgnoreCase))
                return ResolveAudioLPCMFormat(bitrate, frequency, channels);
            if (string.Equals(container, "mp4", StringComparison.OrdinalIgnoreCase))
                return ResolveAudioMP4Format(bitrate, frequency, channels);
            if (string.Equals(container, "adts", StringComparison.OrdinalIgnoreCase))
                return ResolveAudioADTSFormat(bitrate, frequency, channels);
            if (string.Equals(container, "flac", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.FLAC;
            if (string.Equals(container, "oga", StringComparison.OrdinalIgnoreCase) || string.Equals(container, "ogg", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.OGG;
            throw new ArgumentException("Unsupported container: " + container);
        }

        private MediaFormatProfile ResolveAudioASFFormat(int? bitrate, int? frequency, int? channels)
        {
            if (bitrate.HasValue && bitrate.Value <= 193)
            {
                return MediaFormatProfile.WMA_BASE;
            }
            return MediaFormatProfile.WMA_FULL;
        }

        private MediaFormatProfile ResolveAudioLPCMFormat(int? bitrate, int? frequency, int? channels)
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
                if (frequency.Value == 48000 && channels.Value == 1)
                {
                    return MediaFormatProfile.LPCM16_48_STEREO;
                }

                throw new ArgumentException("Unsupported LPCM format of file %s. Only 44100 / 48000 Hz and Mono / Stereo files are allowed.");
            }

            return MediaFormatProfile.LPCM16_48_STEREO;
        }

        private MediaFormatProfile ResolveAudioMP4Format(int? bitrate, int? frequency, int? channels)
        {
            if (bitrate.HasValue && bitrate.Value <= 320)
            {
                return MediaFormatProfile.AAC_ISO_320;
            }
            return MediaFormatProfile.AAC_ISO;
        }

        private MediaFormatProfile ResolveAudioADTSFormat(int? bitrate, int? frequency, int? channels)
        {
            if (bitrate.HasValue && bitrate.Value <= 320)
            {
                return MediaFormatProfile.AAC_ADTS_320;
            }
            return MediaFormatProfile.AAC_ADTS;
        }

        public MediaFormatProfile ResolveImageFormat(string container, int? width, int? height)
        {
            if (string.Equals(container, "jpeg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(container, "jpg", StringComparison.OrdinalIgnoreCase))
                return ResolveImageJPGFormat(width, height);
            if (string.Equals(container, "png", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.PNG_LRG;
            if (string.Equals(container, "gif", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.GIF_LRG;
            if (string.Equals(container, "raw", StringComparison.OrdinalIgnoreCase))
                return MediaFormatProfile.RAW;

            throw new ArgumentException("Unsupported container: " + container);
        }

        private MediaFormatProfile ResolveImageJPGFormat(int? width, int? height)
        {
            if (width.HasValue && height.HasValue)
            {
                if ((width.Value <= 640) && (height.Value <= 480))
                    return MediaFormatProfile.JPEG_SM;

                if ((width.Value <= 1024) && (height.Value <= 768))
                {
                    return MediaFormatProfile.JPEG_MED;
                }
            }

            return MediaFormatProfile.JPEG_LRG;
        }
    }
}
