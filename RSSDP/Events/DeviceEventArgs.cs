using System;
using Rssdp.Devices;

namespace Rssdp.Events
{
    /// <summary>
    /// Event arguments for the <see cref="SsdpDevice.DeviceAdded"/> and <see cref="SsdpDevice.DeviceRemoved"/> events.
    /// </summary>
    public sealed class DeviceEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new instance for the specified <see cref="SsdpDevice"/>.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> associated with the event this argument class is being used for.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
        public DeviceEventArgs(SsdpDevice device)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        /// Returns the <see cref="SsdpDevice"/> instance the event being raised for.
        /// </summary>
        public SsdpDevice Device { get; }
    }
}
