#pragma warning disable SA1300 // Lowercase required for backwards compat.

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Enum containing hardware acceleration types.
/// </summary>
public enum HardwareAccelerationType
{
    /// <summary>
    /// Software acceleration.
    /// </summary>
    none = 0,

    /// <summary>
    /// AMD AMF.
    /// </summary>
    amf = 1,

    /// <summary>
    /// Intel Quick Sync Video.
    /// </summary>
    qsv = 2,

    /// <summary>
    /// NVIDIA NVENC.
    /// </summary>
    nvenc = 3,

    /// <summary>
    /// Video4Linux2 V4L2M2M.
    /// </summary>
    v4l2m2m = 4,

    /// <summary>
    /// Video Acceleration API (VAAPI).
    /// </summary>
    vaapi = 5,

    /// <summary>
    /// Video ToolBox.
    /// </summary>
    videotoolbox = 6,

    /// <summary>
    /// Rockchip Media Process Platform (RKMPP).
    /// </summary>
    rkmpp = 7
}
