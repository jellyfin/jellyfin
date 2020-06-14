using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Devices
{
    public interface IDeviceManager
    {
        /// <summary>
        /// Saves the capabilities.
        /// </summary>
        /// <param name="reportedId">The reported identifier.</param>
        /// <param name="capabilities">The capabilities.</param>
        /// <returns>Task.</returns>
        void SaveCapabilities(string reportedId, ClientCapabilities capabilities);

        /// <summary>
        /// Gets the capabilities.
        /// </summary>
        /// <param name="reportedId">The reported identifier.</param>
        /// <returns>ClientCapabilities.</returns>
        ClientCapabilities GetCapabilities(string reportedId);

        /// <summary>
        /// Gets the device information.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>DeviceInfo.</returns>
        DeviceInfo GetDevice(string id);

        /// <summary>
        /// Gets the devices.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable&lt;DeviceInfo&gt;.</returns>
        QueryResult<DeviceInfo> GetDevices(DeviceQuery query);

        /// <summary>
        /// Determines whether this instance [can access device] the specified user identifier.
        /// </summary>
        bool CanAccessDevice(User user, string deviceId);

        void UpdateDeviceOptions(string deviceId, DeviceOptions options);
        DeviceOptions GetDeviceOptions(string deviceId);
        event EventHandler<GenericEventArgs<Tuple<string, DeviceOptions>>> DeviceOptionsUpdated;
    }
}
