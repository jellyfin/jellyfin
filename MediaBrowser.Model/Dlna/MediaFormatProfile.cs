#pragma warning disable CA1707 // Identifiers should not contain underscores
namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines a value representing MediaFormatProfile.
    /// </summary>
    public enum MediaFormatProfile
    {
        /// <summary>
        /// Defines a value representing MP3.
        /// </summary>
        MP3,

        /// <summary>
        /// Defines a value representing WMA_BASE.
        /// </summary>
        WMA_BASE,

        /// <summary>
        /// Defines a value representing WMA_FULL.
        /// </summary>
        WMA_FULL,

        /// <summary>
        /// Defines a value representing LPCM16_44_MONO.
        /// </summary>
        LPCM16_44_MONO,

        /// <summary>
        /// Defines a value representing LPCM16_44_STEREO.
        /// </summary>
        LPCM16_44_STEREO,

        /// <summary>
        /// Defines a value representing LPCM16_48_MONO.
        /// </summary>
        LPCM16_48_MONO,

        /// <summary>
        /// Defines a value representing LPCM16_48_STEREO.
        /// </summary>
        LPCM16_48_STEREO,

        /// <summary>
        /// Defines a value representing AAC_ISO.
        /// </summary>
        AAC_ISO,

        /// <summary>
        /// Defines a value representing AAC_ISO_320.
        /// </summary>
        AAC_ISO_320,

        /// <summary>
        /// Defines a value representing AAC_ADTS.
        /// </summary>
        AAC_ADTS,

        /// <summary>
        /// Defines a value representing AAC_ADTS_320.
        /// </summary>
        AAC_ADTS_320,

        /// <summary>
        /// Defines a value representing FLAC.
        /// </summary>
        FLAC,

        /// <summary>
        /// Defines a value representing OGG.
        /// </summary>
        OGG,

        /// <summary>
        /// Defines a value representing JPEG_SM.
        /// </summary>
        JPEG_SM,

        /// <summary>
        /// Defines a value representing JPEG_MED.
        /// </summary>
        JPEG_MED,

        /// <summary>
        /// Defines a value representing JPEG_LRG.
        /// </summary>
        JPEG_LRG,

        /// <summary>
        /// Defines a value representing JPEG_TN.
        /// </summary>
        JPEG_TN,

        /// <summary>
        /// Defines a value representing PNG_LRG.
        /// </summary>
        PNG_LRG,

        /// <summary>
        /// Defines a value representing PNG_TN.
        /// </summary>
        PNG_TN,

        /// <summary>
        /// Defines a value representing GIF_LRG.
        /// </summary>
        GIF_LRG,

        /// <summary>
        /// Defines a value representing RAW.
        /// </summary>
        RAW,

        /// <summary>
        /// Defines a value representing MPEG1.
        /// </summary>
        MPEG1,

        /// <summary>
        /// Defines a value representing MPEG_PS_PAL.
        /// </summary>
        MPEG_PS_PAL,

        /// <summary>
        /// Defines a value representing MPEG_PS_NTSC.
        /// </summary>
        MPEG_PS_NTSC,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_EU.
        /// </summary>
        MPEG_TS_SD_EU,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_EU_ISO.
        /// </summary>
        MPEG_TS_SD_EU_ISO,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_EU_T.
        /// </summary>
        MPEG_TS_SD_EU_T,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_NA.
        /// </summary>
        MPEG_TS_SD_NA,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_NA_ISO.
        /// </summary>
        MPEG_TS_SD_NA_ISO,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_NA_T.
        /// </summary>
        MPEG_TS_SD_NA_T,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_KO.
        /// </summary>
        MPEG_TS_SD_KO,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_KO_ISO.
        /// </summary>
        MPEG_TS_SD_KO_ISO,

        /// <summary>
        /// Defines a value representing MPEG_TS_SD_KO_T.
        /// </summary>
        MPEG_TS_SD_KO_T,

        /// <summary>
        /// Defines a value representing MPEG_TS_JP_T.
        /// </summary>
        MPEG_TS_JP_T,

        /// <summary>
        /// Defines a value representing AVI.
        /// </summary>
        AVI,

        /// <summary>
        /// Defines a value representing MATROSKA.
        /// </summary>
        MATROSKA,

        /// <summary>
        /// Defines a value representing FLV.
        /// </summary>
        FLV,

        /// <summary>
        /// Defines a value representing DVR_MS.
        /// </summary>
        DVR_MS,

        /// <summary>
        /// Defines a value representing WTV.
        /// </summary>
        WTV,

        /// <summary>
        /// Defines a value representing OGV.
        /// </summary>
        OGV,

        /// <summary>
        /// Defines a value representing AVC_MP4_MP_SD_AAC_MULT5.
        /// </summary>
        AVC_MP4_MP_SD_AAC_MULT5,

        /// <summary>
        /// Defines a value representing AVC_MP4_MP_SD_MPEG1_L3.
        /// </summary>
        AVC_MP4_MP_SD_MPEG1_L3,

        /// <summary>
        /// Defines a value representing AVC_MP4_MP_SD_AC3.
        /// </summary>
        AVC_MP4_MP_SD_AC3,

        /// <summary>
        /// Defines a value representing AVC_MP4_MP_HD_720p_AAC.
        /// </summary>
        AVC_MP4_MP_HD_720p_AAC,

        /// <summary>
        /// Defines a value representing AVC_MP4_MP_HD_1080i_AAC.
        /// </summary>
        AVC_MP4_MP_HD_1080i_AAC,

        /// <summary>
        /// Defines a value representing AVC_MP4_HP_HD_AAC.
        /// </summary>
        AVC_MP4_HP_HD_AAC,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_AAC_MULT5.
        /// </summary>
        AVC_TS_MP_HD_AAC_MULT5,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_AAC_MULT5_T.
        /// </summary>
        AVC_TS_MP_HD_AAC_MULT5_T,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_AAC_MULT5_ISO.
        /// </summary>
        AVC_TS_MP_HD_AAC_MULT5_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_MPEG1_L3.
        /// </summary>
        AVC_TS_MP_HD_MPEG1_L3,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_MPEG1_L3_T.
        /// </summary>
        AVC_TS_MP_HD_MPEG1_L3_T,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_MPEG1_L3_ISO.
        /// </summary>
        AVC_TS_MP_HD_MPEG1_L3_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_AC3.
        /// </summary>
        AVC_TS_MP_HD_AC3,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_AC3_T.
        /// </summary>
        AVC_TS_MP_HD_AC3_T,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_HD_AC3_ISO.
        /// </summary>
        AVC_TS_MP_HD_AC3_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_HP_HD_MPEG1_L2_T.
        /// </summary>
        AVC_TS_HP_HD_MPEG1_L2_T,

        /// <summary>
        /// Defines a value representing AVC_TS_HP_HD_MPEG1_L2_ISO.
        /// </summary>
        AVC_TS_HP_HD_MPEG1_L2_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_AAC_MULT5.
        /// </summary>
        AVC_TS_MP_SD_AAC_MULT5,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_AAC_MULT5_T.
        /// </summary>
        AVC_TS_MP_SD_AAC_MULT5_T,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_AAC_MULT5_ISO.
        /// </summary>
        AVC_TS_MP_SD_AAC_MULT5_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_MPEG1_L3.
        /// </summary>
        AVC_TS_MP_SD_MPEG1_L3,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_MPEG1_L3_T.
        /// </summary>
        AVC_TS_MP_SD_MPEG1_L3_T,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_MPEG1_L3_ISO.
        /// </summary>
        AVC_TS_MP_SD_MPEG1_L3_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_HP_SD_MPEG1_L2_T.
        /// </summary>
        AVC_TS_HP_SD_MPEG1_L2_T,

        /// <summary>
        /// Defines a value representing AVC_TS_HP_SD_MPEG1_L2_ISO.
        /// </summary>
        AVC_TS_HP_SD_MPEG1_L2_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_AC3.
        /// </summary>
        AVC_TS_MP_SD_AC3,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_AC3_T.
        /// </summary>
        AVC_TS_MP_SD_AC3_T,

        /// <summary>
        /// Defines a value representing AVC_TS_MP_SD_AC3_ISO.
        /// </summary>
        AVC_TS_MP_SD_AC3_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_HD_DTS_T.
        /// </summary>
        AVC_TS_HD_DTS_T,

        /// <summary>
        /// Defines a value representing AVC_TS_HD_DTS_ISO.
        /// </summary>
        AVC_TS_HD_DTS_ISO,

        /// <summary>
        /// Defines a value representing WMVMED_BASE.
        /// </summary>
        WMVMED_BASE,

        /// <summary>
        /// Defines a value representing WMVMED_FULL.
        /// </summary>
        WMVMED_FULL,

        /// <summary>
        /// Defines a value representing WMVMED_PRO.
        /// </summary>
        WMVMED_PRO,

        /// <summary>
        /// Defines a value representing WMVHIGH_FULL.
        /// </summary>
        WMVHIGH_FULL,

        /// <summary>
        /// Defines a value representing WMVHIGH_PRO.
        /// </summary>
        WMVHIGH_PRO,

        /// <summary>
        /// Defines a value representing VC1_ASF_AP_L1_WMA.
        /// </summary>
        VC1_ASF_AP_L1_WMA,

        /// <summary>
        /// Defines a value representing VC1_ASF_AP_L2_WMA.
        /// </summary>
        VC1_ASF_AP_L2_WMA,

        /// <summary>
        /// Defines a value representing VC1_ASF_AP_L3_WMA.
        /// </summary>
        VC1_ASF_AP_L3_WMA,

        /// <summary>
        /// Defines a value representing VC1_TS_AP_L1_AC3_ISO.
        /// </summary>
        VC1_TS_AP_L1_AC3_ISO,

        /// <summary>
        /// Defines a value representing VC1_TS_AP_L2_AC3_ISO.
        /// </summary>
        VC1_TS_AP_L2_AC3_ISO,

        /// <summary>
        /// Defines a value representing VC1_TS_HD_DTS_ISO.
        /// </summary>
        VC1_TS_HD_DTS_ISO,

        /// <summary>
        /// Defines a value representing VC1_TS_HD_DTS_T.
        /// </summary>
        VC1_TS_HD_DTS_T,

        /// <summary>
        /// Defines a value representing MPEG4_P2_MP4_ASP_AAC.
        /// </summary>
        MPEG4_P2_MP4_ASP_AAC,

        /// <summary>
        /// Defines a value representing MPEG4_P2_MP4_SP_L6_AAC.
        /// </summary>
        MPEG4_P2_MP4_SP_L6_AAC,

        /// <summary>
        /// Defines a value representing MPEG4_P2_MP4_NDSD.
        /// </summary>
        MPEG4_P2_MP4_NDSD,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_AAC.
        /// </summary>
        MPEG4_P2_TS_ASP_AAC,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_AAC_T.
        /// </summary>
        MPEG4_P2_TS_ASP_AAC_T,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_AAC_ISO.
        /// </summary>
        MPEG4_P2_TS_ASP_AAC_ISO,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_MPEG1_L3.
        /// </summary>
        MPEG4_P2_TS_ASP_MPEG1_L3,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_MPEG1_L3_T.
        /// </summary>
        MPEG4_P2_TS_ASP_MPEG1_L3_T,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_MPEG1_L3_ISO.
        /// </summary>
        MPEG4_P2_TS_ASP_MPEG1_L3_ISO,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_MPEG2_L2.
        /// </summary>
        MPEG4_P2_TS_ASP_MPEG2_L2,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_MPEG2_L2_T.
        /// </summary>
        MPEG4_P2_TS_ASP_MPEG2_L2_T,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_MPEG2_L2_ISO.
        /// </summary>
        MPEG4_P2_TS_ASP_MPEG2_L2_ISO,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_AC3.
        /// </summary>
        MPEG4_P2_TS_ASP_AC3,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_AC3_T.
        /// </summary>
        MPEG4_P2_TS_ASP_AC3_T,

        /// <summary>
        /// Defines a value representing MPEG4_P2_TS_ASP_AC3_ISO.
        /// </summary>
        MPEG4_P2_TS_ASP_AC3_ISO,

        /// <summary>
        /// Defines a value representing AVC_TS_HD_50_LPCM_T.
        /// </summary>
        AVC_TS_HD_50_LPCM_T,

        /// <summary>
        /// Defines a value representing AVC_MP4_LPCM.
        /// </summary>
        AVC_MP4_LPCM,

        /// <summary>
        /// Defines a value representing MPEG4_P2_3GPP_SP_L0B_AAC.
        /// </summary>
        MPEG4_P2_3GPP_SP_L0B_AAC,

        /// <summary>
        /// Defines a value representing MPEG4_P2_3GPP_SP_L0B_AMR.
        /// </summary>
        MPEG4_P2_3GPP_SP_L0B_AMR,

        /// <summary>
        /// Defines a value representing AVC_3GPP_BL_QCIF15_AAC.
        /// </summary>
        AVC_3GPP_BL_QCIF15_AAC,

        /// <summary>
        /// Defines a value representing MPEG4_H263_3GPP_P0_L10_AMR.
        /// </summary>
        MPEG4_H263_3GPP_P0_L10_AMR,

        /// <summary>
        /// Defines a value representing MPEG4_H263_MP4_P0_L10_AAC.
        /// </summary>
        MPEG4_H263_MP4_P0_L10_AAC
    }
}
