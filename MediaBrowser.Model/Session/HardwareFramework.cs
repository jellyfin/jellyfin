namespace MediaBrowser.Model.Session;

/// <summary>
/// Enum representing the hardware framework a transcoding pipeline stage runs on.
/// </summary>
public enum HardwareFramework
{
    /// <summary>
    /// Software (CPU) processing.
    /// </summary>
    Software = 0,

    /// <summary>
    /// Intel Quick Sync Video.
    /// </summary>
    Qsv = 1,

    /// <summary>
    /// NVIDIA CUDA/NVENC/NPP.
    /// </summary>
    Cuda = 2,

    /// <summary>
    /// Video Acceleration API (VAAPI).
    /// </summary>
    Vaapi = 3,

    /// <summary>
    /// Direct3D 11 Video Acceleration.
    /// </summary>
    D3D11Va = 4,

    /// <summary>
    /// Apple VideoToolbox.
    /// </summary>
    VideoToolbox = 5,

    /// <summary>
    /// AMD Advanced Media Framework.
    /// </summary>
    Amf = 6,

    /// <summary>
    /// OpenCL.
    /// </summary>
    OpenCl = 7,

    /// <summary>
    /// Vulkan.
    /// </summary>
    Vulkan = 8,

    /// <summary>
    /// Rockchip Media Process Platform (RKMPP/RKRGA).
    /// </summary>
    Rkmpp = 9,

    /// <summary>
    /// Apple AudioToolbox (hardware-accelerated audio, e.g. aac_at).
    /// </summary>
    AudioToolbox = 10,

    /// <summary>
    /// Video4Linux2 Memory-to-Memory (V4L2 M2M, e.g. h264_v4l2m2m).
    /// </summary>
    V4l2m2m = 11,

    /// <summary>
    /// DirectX Video Acceleration 2.
    /// </summary>
    Dxva2 = 12
}
