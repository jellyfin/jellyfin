using System;

namespace Jellyfin.Networking.Dlna
{
    /// <summary>
    /// Event arguments for the <see cref="SsdpDevice.DeviceAdded"/> and <see cref="SsdpDevice.DeviceRemoved"/> events.
    /// </summary>
    /// <remarks>
    /// Part of this code take from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public sealed class DeviceEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceEventArgs"/> class.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> associated with the event this argument class is being used for.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
        public DeviceEventArgs(SsdpDevice device)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        /// Gets the <see cref="SsdpDevice"/> instance the event being raised for.
        /// </summary>
        public SsdpDevice Device { get; }
    }
}
