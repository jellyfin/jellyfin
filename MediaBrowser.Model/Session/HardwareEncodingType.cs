namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Enum HardwareEncodingType.
    /// </summary>
    public enum HardwareEncodingType
    {
        /// <summary>
        /// AMD AMF
        /// </summary>
        AMF,

        /// <summary>
        /// Intel Quick Sync Video
        /// </summary>
        QSV,

        /// <summary>
        /// NVIDIA NVENC
        /// </summary>
        NVENC,

        /// <summary>
        /// OpenMax OMX
        /// </summary>
        OMX,

        /// <summary>
        /// Exynos V4L2 MFC
        /// </summary>
        V4L2M2M,

        /// <summary>
        /// MediaCodec Android
        /// </summary>
        MediaCodec,

        /// <summary>
        /// Video Acceleration API (VAAPI)
        /// </summary>
        VAAPI,

        /// <summary>
        /// Video ToolBox
        /// </summary>
        VideoToolBox
    }
}
