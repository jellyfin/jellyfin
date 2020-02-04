namespace Rssdp
{
    /// <summary>
    /// Represents a device that is a descendant of a <see cref="SsdpRootDevice"/> instance.
    /// </summary>
    public class SsdpEmbeddedDevice : SsdpDevice
    {

        #region Fields
        private SsdpRootDevice _RootDevice;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SsdpEmbeddedDevice()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns the <see cref="SsdpRootDevice"/> that is this device's first ancestor. If this device is itself an <see cref="SsdpRootDevice"/>, then returns a reference to itself.
        /// </summary>
        public SsdpRootDevice RootDevice
        {
            get
            {
                return _RootDevice;
            }
            internal set
            {
                _RootDevice = value;
                lock (this.Devices)
                {
                    foreach (var embeddedDevice in this.Devices)
                    {
                        ((SsdpEmbeddedDevice)embeddedDevice).RootDevice = _RootDevice;
                    }
                }
            }
        }

        #endregion
    }
}
