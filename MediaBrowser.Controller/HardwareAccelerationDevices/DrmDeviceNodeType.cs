namespace MediaBrowser.Controller.HardwareAccelerationDevices
{
    /// <summary>
    /// Linux DRM device type.
    /// </summary>
    public enum DrmDeviceNodeType
    {
        /// <summary>
        /// Primary DRM node.
        /// /dev/dri/card1.
        /// </summary>
        Primary = 1,

        /// <summary>
        /// Render DRM node.
        /// /dev/dri/renderD129.
        /// </summary>
        Render = 2
    }
}
