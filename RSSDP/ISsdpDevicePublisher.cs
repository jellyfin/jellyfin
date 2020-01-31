using System.Threading.Tasks;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Interface for components that publish the existence of SSDP devices.
    /// </summary>
    /// <remarks>
    /// <para>Publishing a device includes sending notifications (alive and byebye) as well as responding to search requests when appropriate.</para>
    /// </remarks>
    /// <seealso cref="SsdpRootDevice"/>
    /// <seealso cref="ISsdpDeviceLocator"/>
    public interface ISsdpDevicePublisher
    {
        /// <summary>
        /// Adds a device (and it's children) to the list of devices being published by this server, making them discoverable to SSDP clients.
        /// </summary>
        /// <param name="device">The <see cref="SsdpRootDevice"/> instance to add.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        void AddDevice(SsdpRootDevice device);

        /// <summary>
        /// Removes a device (and it's children) from the list of devices being published by this server, making them undiscoverable.
        /// </summary>
        /// <param name="device">The <see cref="SsdpRootDevice"/> instance to add.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task RemoveDevice(SsdpRootDevice device);

        /// <summary>
        /// Returns a read only list of devices being published by this instance.
        /// </summary>
        /// <seealso cref="SsdpDevice"/>
        System.Collections.Generic.IEnumerable<SsdpRootDevice> Devices { get; }
    }
}
