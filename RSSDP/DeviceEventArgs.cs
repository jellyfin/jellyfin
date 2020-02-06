using System;

namespace Rssdp
{
    /// <summary>
    /// Event arguments for the <see cref="SsdpDevice.DeviceAdded"/> and <see cref="SsdpDevice.DeviceRemoved"/> events.
    /// </summary>
    public sealed class DeviceEventArgs : EventArgs
    {

        #region Fields

        private readonly SsdpDevice _Device;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance for the specified <see cref="SsdpDevice"/>.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> associated with the event this argument class is being used for.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
        public DeviceEventArgs(SsdpDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            _Device = device;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns the <see cref="SsdpDevice"/> instance the event being raised for.
        /// </summary>
        public SsdpDevice Device
        {
            get { return _Device; }
        }

        #endregion

    }
}
