namespace MediaBrowser.Controller.HardwareAccelerationDevices
{
    /// <summary>
    /// Linux direct rendering device node.
    /// </summary>
    public class DrmDeviceNode
    {
        /// <summary>
        /// Gets DRM device node file full path.
        /// Example: "/dev/dri/renderD128", "/dev/dri/card1".
        /// </summary>
        public required string DrmDeviceNodeFileFullPath { get; init; }

        /// <summary>
        /// Gets PCI device directory full path. Uniquely identifies the device in the system.
        /// Example: "/sys/devices/pci0000:00/0000:00:01.0/0000:01:00.0/0000:02:00.0/0000:03:00.0".
        /// </summary>
        public required string HardwareDeviceSysfsFullPath { get; init; }

        /// <summary>
        /// Gets DRM device node type.
        /// </summary>
        public required DrmDeviceNodeType DrmDeviceNodeType { get; init; }
    }
}
