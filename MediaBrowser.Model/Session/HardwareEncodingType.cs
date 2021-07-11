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
        AMF = 0,

        /// <summary>
        /// Intel Quick Sync Video
        /// </summary>
        QSV = 1,

        /// <summary>
        /// NVIDIA NVENC
        /// </summary>
        NVENC = 2,

        /// <summary>
        /// OpenMax OMX
        /// </summary>
        OMX = 3,

        /// <summary>
        /// Exynos V4L2 MFC
        /// </summary>
        V4L2M2M = 4,

        /// <summary>
        /// MediaCodec Android
        /// </summary>
        MediaCodec = 5,

        /// <summary>
        /// Video Acceleration API (VAAPI)
        /// </summary>
        VAAPI = 6,

        /// <summary>
        /// Video ToolBox
        /// </summary>
        VideoToolBox = 7
    }
}
