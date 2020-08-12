#nullable enable
using System;

namespace Emby.Dlna.Rssdp.Devices
{
    /// <summary>
    /// Represents a device that is a descendant of a <see cref="SsdpRootDevice"/> instance.
    /// </summary>
    public class SsdpEmbeddedDevice : SsdpDevice
    {
        private SsdpRootDevice? _rootDevice;

        public SsdpEmbeddedDevice(SsdpRootDevice rootDevice, string udn)
            : base(rootDevice?.FriendlyName ?? throw new ArgumentNullException(nameof(rootDevice)), rootDevice.Manufacturer, rootDevice.ModelName, udn)
        {
        }

        /// <summary>
        /// Gets the <see cref="SsdpRootDevice"/> that is this device's first ancestor. If this device is itself an <see cref="SsdpRootDevice"/>, then returns a reference to itself.
        /// </summary>
        public SsdpRootDevice? RootDevice
        {
            get
            {
                return _rootDevice;
            }

            internal set
            {
                _rootDevice = value;
                ChangeRoot(value);
            }
        }
    }
}
