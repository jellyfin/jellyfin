namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Enum FilterOptionType.
    /// </summary>
    public enum FilterOptionType
    {
        /// <summary>
        /// The scale_cuda_format.
        /// </summary>
        ScaleCudaFormat = 0,

        /// <summary>
        /// The tonemap_cuda_name.
        /// </summary>
        TonemapCudaName = 1,

        /// <summary>
        /// The tonemap_opencl_bt2390.
        /// </summary>
        TonemapOpenclBt2390 = 2,

        /// <summary>
        /// The overlay_opencl_framesync.
        /// </summary>
        OverlayOpenclFrameSync = 3,

        /// <summary>
        /// The overlay_vaapi_framesync.
        /// </summary>
        OverlayVaapiFrameSync = 4,

        /// <summary>
        /// The overlay_vulkan_framesync.
        /// </summary>
        OverlayVulkanFrameSync = 5,

        /// <summary>
        /// The transpose_opencl_reversal.
        /// </summary>
        TransposeOpenclReversal = 6
    }
}
