#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Data.Events;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Devices
{
    public interface IDeviceManager
    {
        event EventHandler<GenericEventArgs<Tuple<string, DeviceOptions>>> DeviceOptionsUpdated;

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
        /// Gets the devices.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable&lt;DeviceInfo&gt;.</returns>
        Task<QueryResult<DeviceInfo>> GetDevices(DeviceQuery query);

        /// <summary>
        /// Determines whether this instance [can access device] the specified user identifier.
        /// </summary>
        bool CanAccessDevice(User user, string deviceId);

        Task UpdateDeviceOptions(string deviceId, DeviceOptions options);

        Task<DeviceOptions> GetDeviceOptions(string deviceId);
    }
}
