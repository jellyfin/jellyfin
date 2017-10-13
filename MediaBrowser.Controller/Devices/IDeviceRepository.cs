using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Session;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Devices
{
    public interface IDeviceRepository
    {
        /// <summary>
        /// Registers the device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns>Task.</returns>
        void SaveDevice(DeviceInfo device);

        /// <summary>
        /// Saves the capabilities.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="capabilities">The capabilities.</param>
        /// <returns>Task.</returns>
        void SaveCapabilities(string id, ClientCapabilities capabilities);

        /// <summary>
        /// Gets the capabilities.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>ClientCapabilities.</returns>
        ClientCapabilities GetCapabilities(string id);

        /// <summary>
        /// Gets the device information.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>DeviceInfo.</returns>
        DeviceInfo GetDevice(string id);

        List<DeviceInfo> GetDevices();

        /// <summary>
        /// Deletes the device.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        void DeleteDevice(string id);

        /// <summary>
        /// Gets the upload history.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>ContentUploadHistory.</returns>
        ContentUploadHistory GetCameraUploadHistory(string deviceId);

        /// <summary>
        /// Saves the camera upload history.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="file">The file.</param>
        void AddCameraUpload(string deviceId, LocalFileInfo file);
    }
}
