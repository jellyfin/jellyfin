using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.HardwareAccelerationDevices
{
    /// <summary>
    /// Searches for Linux DRM device nodes.
    /// </summary>
    public interface ILinuxDrmDeviceNodesDiscoverer
    {
        /// <summary>
        /// Discovers Linux DRM device nodes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Linux DRM device nodes.</returns>
        Task<IEnumerable<DrmDeviceNode>> DiscoverDrmDeviceNodesAsync(CancellationToken cancellationToken);
    }
}
