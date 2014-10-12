using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Session;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Devices
{
    public interface IDeviceManager
    {
        /// <summary>
        /// Registers the device.
        /// </summary>
        /// <param name="reportedId">The reported identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="appName">Name of the application.</param>
        /// <param name="usedByUserId">The used by user identifier.</param>
        /// <returns>Task.</returns>
        Task RegisterDevice(string reportedId, string name, string appName, string usedByUserId);

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
        /// Gets the devices.
        /// </summary>
        /// <returns>IEnumerable&lt;DeviceInfo&gt;.</returns>
        IEnumerable<DeviceInfo> GetDevices();

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
    }
}
