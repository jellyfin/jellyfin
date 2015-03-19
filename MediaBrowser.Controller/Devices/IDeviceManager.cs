using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Devices
{
    public interface IDeviceManager
    {
        /// <summary>
        /// Occurs when [device options updated].
        /// </summary>
        event EventHandler<GenericEventArgs<DeviceInfo>> DeviceOptionsUpdated;
        /// <summary>
        /// Occurs when [camera image uploaded].
        /// </summary>
        event EventHandler<GenericEventArgs<CameraImageUploadInfo>> CameraImageUploaded;

        /// <summary>
        /// Registers the device.
        /// </summary>
        /// <param name="reportedId">The reported identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="appName">Name of the application.</param>
        /// <param name="appVersion">The application version.</param>
        /// <param name="usedByUserId">The used by user identifier.</param>
        /// <returns>Task.</returns>
        Task<DeviceInfo> RegisterDevice(string reportedId, string name, string appName, string appVersion, string usedByUserId);

        /// <summary>
        /// Saves the capabilities.
        /// </summary>
        /// <param name="reportedId">The reported identifier.</param>
        /// <param name="capabilities">The capabilities.</param>
        /// <returns>Task.</returns>
        Task SaveCapabilities(string reportedId, ClientCapabilities capabilities);

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
        /// Updates the device information.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="options">The options.</param>
        /// <returns>Task.</returns>
        Task UpdateDeviceInfo(string id, DeviceOptions options);

        /// <summary>
        /// Gets the devices.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable&lt;DeviceInfo&gt;.</returns>
        QueryResult<DeviceInfo> GetDevices(DeviceQuery query);

        /// <summary>
        /// Deletes the device.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task DeleteDevice(string id);

        /// <summary>
        /// Gets the upload history.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>ContentUploadHistory.</returns>
        ContentUploadHistory GetCameraUploadHistory(string deviceId);

        /// <summary>
        /// Accepts the upload.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="file">The file.</param>
        /// <returns>Task.</returns>
        Task AcceptCameraUpload(string deviceId, Stream stream, LocalFileInfo file);

        /// <summary>
        /// Determines whether this instance [can access device] the specified user identifier.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns><c>true</c> if this instance [can access device] the specified user identifier; otherwise, <c>false</c>.</returns>
        bool CanAccessDevice(string userId, string deviceId);
    }
}
