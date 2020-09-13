#pragma warning disable CS1591
#nullable enable
using System;

namespace Emby.Dlna.PlayTo.Devices
{
    /// <summary>
    /// Represents a device that is a descendant of a <see cref="SsdpRootDevice"/> instance.
    /// </summary>
    /// <remarks>
    /// Part of this code are taken from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public class SsdpEmbeddedDevice : SsdpDevice
    {
        private SsdpRootDevice? _rootDevice;

        public SsdpEmbeddedDevice(string friendlyName, string manufacturer, string modelName, string uuid)
        : base(friendlyName, manufacturer, modelName, uuid)
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
