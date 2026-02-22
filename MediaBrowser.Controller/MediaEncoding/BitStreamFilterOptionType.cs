namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Enum BitStreamFilterOptionType.
/// </summary>
public enum BitStreamFilterOptionType
{
    /// <summary>
    /// hevc_metadata bsf with remove_dovi option.
    /// </summary>
    HevcMetadataRemoveDovi = 0,

    /// <summary>
    /// hevc_metadata bsf with remove_hdr10plus option.
    /// </summary>
    HevcMetadataRemoveHdr10Plus = 1,

    /// <summary>
    /// av1_metadata bsf with remove_dovi option.
    /// </summary>
    Av1MetadataRemoveDovi = 2,

    /// <summary>
    /// av1_metadata bsf with remove_hdr10plus option.
    /// </summary>
    Av1MetadataRemoveHdr10Plus = 3,

    /// <summary>
    /// dovi_rpu bsf with strip option.
    /// </summary>
    DoviRpuStrip = 4,
}
