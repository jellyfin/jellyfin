using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.HardwareAccelerationDevices
{
    /// <summary>
    /// Detects and collects information about hardware acceleration devices of a specific type.
    /// </summary>
    public interface IHardwareAccelerationTypeDeviceDiscoverer
    {
        /// <summary>
        /// Gets hardware acceleration type.
        /// </summary>
        HardwareAccelerationType HardwareAccelerationType { get; }

        /// <summary>
        /// Searches for hardware acceleration devices.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of hardware acceleration devices of its type.</returns>
        Task<IEnumerable<HardwareAccelerationDevice>> DiscoverDevicesAsync(CancellationToken cancellationToken);
    }
}
