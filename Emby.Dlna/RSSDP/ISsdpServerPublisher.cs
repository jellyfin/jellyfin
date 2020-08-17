using System.Collections.Generic;
using System.Threading.Tasks;
using Emby.Dlna.Rssdp.Devices;

namespace Emby.Dlna.Rssdp
{
    /// <summary>
    /// Interface for components that publish the existence of SSDP devices.
    /// </summary>
    /// <remarks>
    /// <para>Publishing a device includes sending notifications (alive and byebye) as well as responding to search requests when appropriate.</para>
    /// </remarks>
    public interface ISsdpServerPublisher
    {
        /// <summary>
        /// Gets returns a read only list of devices being published by this instance.
        /// </summary>
        /// <seealso cref="SsdpDevice"/>
        IEnumerable<SsdpRootDevice> Devices { get; }

        /// <summary>
        /// Adds a device (and it's children) to the list of devices being published by this server, making them discoverable to SSDP clients.
        /// </summary>
        /// <param name="device">The <see cref="SsdpRootDevice"/> instance to add.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task AddDevice(SsdpRootDevice device);
    }
}
