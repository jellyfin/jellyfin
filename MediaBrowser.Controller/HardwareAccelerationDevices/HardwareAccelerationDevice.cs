using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.HardwareAccelerationDevices
{
    /// <summary>
    /// Hardware acceleration device.
    /// </summary>
    public class HardwareAccelerationDevice
    {
        /// <summary>
        /// Gets the human-readable name.
        /// </summary>
        /// <value>Human-readable name.</value>
        public required string HumanReadableName { get; init; }

        /// <summary>
        /// Gets the hardware acceleration type.
        /// </summary>
        /// <value>Hardware acceleration type.</value>
        public required HardwareAccelerationType HardwareAccelerationType { get; init; }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public required string Identifier { get; init; }
    }
}
