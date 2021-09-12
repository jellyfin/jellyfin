#nullable disable

#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Devices
{
    public interface IDeviceManager
    {
        event EventHandler<GenericEventArgs<Tuple<string, DeviceOptions>>> DeviceOptionsUpdated;

        /// <summary>
        /// Creates a new device.
        /// </summary>
        /// <param name="device">The device to create.</param>
        /// <returns>A <see cref="Task{Device}"/> representing the creation of the device.</returns>
        Task<Device> CreateDevice(Device device);

        /// <summary>
        /// Saves the capabilities.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="capabilities">The capabilities.</param>
        void SaveCapabilities(string deviceId, ClientCapabilities capabilities);

        /// <summary>
        /// Gets the capabilities.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <returns>ClientCapabilities.</returns>
        ClientCapabilities GetCapabilities(string deviceId);

        /// <summary>
        /// Gets the device information.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>DeviceInfo.</returns>
        Task<DeviceInfo> GetDevice(string id);

        /// <summary>
        /// Gets devices based on the provided query.
        /// </summary>
        /// <param name="query">The device query.</param>
        /// <returns>A <see cref="Task{QueryResult}"/> representing the retrieval of the devices.</returns>
        Task<QueryResult<Device>> GetDevices(DeviceQuery query);

        Task<QueryResult<DeviceInfo>> GetDeviceInfos(DeviceQuery query);

        /// <summary>
        /// Gets the devices.
        /// </summary>
        /// <param name="userId">The user's id, or <c>null</c>.</param>
        /// <param name="supportsSync">A value indicating whether the device supports sync, or <c>null</c>.</param>
        /// <returns>IEnumerable&lt;DeviceInfo&gt;.</returns>
        Task<QueryResult<DeviceInfo>> GetDevicesForUser(Guid? userId, bool? supportsSync);

        Task DeleteDevice(Device device);

        /// <summary>
        /// Determines whether this instance [can access device] the specified user identifier.
        /// </summary>
        /// <param name="user">The user to test.</param>
        /// <param name="deviceId">The device id to test.</param>
        /// <returns>Whether the user can access the device.</returns>
        bool CanAccessDevice(User user, string deviceId);

        Task UpdateDeviceOptions(string deviceId, string deviceName);

        Task<DeviceOptions> GetDeviceOptions(string deviceId);
    }
}
