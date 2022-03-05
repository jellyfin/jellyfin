namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Enum HardwareEncodingType.
    /// </summary>
    public enum HardwareEncodingType
    {
        /// <summary>
        /// AMD AMF.
        /// </summary>
        AMF = 0,

        /// <summary>
        /// Intel Quick Sync Video.
        /// </summary>
        QSV = 1,

        /// <summary>
        /// NVIDIA NVENC.
        /// </summary>
        NVENC = 2,

        /// <summary>
        /// Video4Linux2 V4L2.
        /// </summary>
        V4L2M2M = 3,

        /// <summary>
        /// Video Acceleration API (VAAPI).
        /// </summary>
        VAAPI = 4,

        /// <summary>
        /// Video ToolBox.
        /// </summary>
        VideoToolBox = 5
    }
}
