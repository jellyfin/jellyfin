namespace Jellyfin.Data.Enums;

/// <summary>
/// An enum representing types of video ranges.
/// </summary>
public enum VideoRangeType
{
    /// <summary>
    /// Unknown video range type.
    /// </summary>
    Unknown,

    /// <summary>
    /// SDR video range type (8bit).
    /// </summary>
    SDR,

    /// <summary>
    /// HDR10 video range type (10bit).
    /// </summary>
    HDR10,

    /// <summary>
    /// HLG video range type (10bit).
    /// </summary>
    HLG,

    /// <summary>
    /// Dolby Vision video range type (10bit encoded / 12bit remapped).
    /// </summary>
    DOVI,

    /// <summary>
    /// Dolby Vision with HDR10 video range fallback (10bit).
    /// </summary>
    DOVIWithHDR10,

    /// <summary>
    /// Dolby Vision with HLG video range fallback (10bit).
    /// </summary>
    DOVIWithHLG,

    /// <summary>
    /// Dolby Vision with SDR video range fallback (8bit / 10bit).
    /// </summary>
    DOVIWithSDR,

    /// <summary>
    /// Dolby Vision with Enhancment Layer (Profile 7).
    /// </summary>
    DOVIWithEL,

    /// <summary>
    /// Dolby Vision and HDR10+ Metadata coexists.
    /// </summary>
    DOVIWithHDR10Plus,

    /// <summary>
    /// Dolby Vision with Enhancment Layer (Profile 7) and HDR10+ Metadata coexists.
    /// </summary>
    DOVIWithELHDR10Plus,

    /// <summary>
    /// Dolby Vision with invalid configuration. e.g. Profile 8 compat id 6.
    /// When using this range, the server would assume the video is still HDR10 after removing the Dolby Vision metadata.
    /// </summary>
    DOVIInvalid,

    /// <summary>
    /// HDR10+ video range type (10bit to 16bit).
    /// </summary>
    HDR10Plus
}
