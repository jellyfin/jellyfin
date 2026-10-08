using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.HardwareAccelerationDevices
{
    /// <summary>
    /// A class that manages the list of hardware acceleration devices.
    /// </summary>
    public interface IHardwareAccelerationDeviceManager
    {
        /// <summary>
        /// Gets devices grouped by hardware acceleration type.
        /// </summary>
        IReadOnlyDictionary<HardwareAccelerationType, IEnumerable<HardwareAccelerationDevice>> Devices { get; }

        /// <summary>
        /// Updates information about available devices.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RefreshAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Updates information about available devices.
        /// </summary>
        /// <param name="type">Hardware acceleration type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RefreshConcreteTypeAsync(HardwareAccelerationType type, CancellationToken cancellationToken);
    }
}
