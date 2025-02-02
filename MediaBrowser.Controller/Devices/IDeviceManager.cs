using System;
using System.Threading.Tasks;
using Jellyfin.Data.Dtos;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Devices;

/// <summary>
/// Device manager interface.
/// </summary>
public interface IDeviceManager
{
    /// <summary>
    /// Event handler for updated device options.
    /// </summary>
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
    ClientCapabilities GetCapabilities(string? deviceId);

    /// <summary>
    /// Gets the device information.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>DeviceInfoDto.</returns>
    DeviceInfoDto? GetDevice(string id);

    /// <summary>
    /// Gets devices based on the provided query.
    /// </summary>
    /// <param name="query">The device query.</param>
    /// <returns>A <see cref="Task{QueryResult}"/> representing the retrieval of the devices.</returns>
    QueryResult<Device> GetDevices(DeviceQuery query);

    /// <summary>
    /// Gets device information based on the provided query.
    /// </summary>
    /// <param name="query">The device query.</param>
    /// <returns>A <see cref="Task{QueryResult}"/> representing the retrieval of the device information.</returns>
    QueryResult<DeviceInfo> GetDeviceInfos(DeviceQuery query);

    /// <summary>
    /// Gets the device information.
    /// </summary>
    /// <param name="userId">The user's id, or <c>null</c>.</param>
    /// <returns>IEnumerable&lt;DeviceInfoDto&gt;.</returns>
    QueryResult<DeviceInfoDto> GetDevicesForUser(Guid? userId);

    /// <summary>
    /// Deletes a device.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <returns>A <see cref="Task"/> representing the deletion of the device.</returns>
    Task DeleteDevice(Device device);

    /// <summary>
    /// Updates a device.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <returns>A <see cref="Task"/> representing the update of the device.</returns>
    Task UpdateDevice(Device device);

    /// <summary>
    /// Determines whether this instance [can access device] the specified user identifier.
    /// </summary>
    /// <param name="user">The user to test.</param>
    /// <param name="deviceId">The device id to test.</param>
    /// <returns>Whether the user can access the device.</returns>
    bool CanAccessDevice(User user, string deviceId);

    /// <summary>
    /// Updates the options of a device.
    /// </summary>
    /// <param name="deviceId">The device id.</param>
    /// <param name="deviceName">The device name.</param>
    /// <returns>A <see cref="Task"/> representing the update of the device options.</returns>
    Task UpdateDeviceOptions(string deviceId, string? deviceName);

    /// <summary>
    /// Gets the options of a device.
    /// </summary>
    /// <param name="deviceId">The device id.</param>
    /// <returns><see cref="DeviceOptions"/> of the device.</returns>
    DeviceOptionsDto? GetDeviceOptions(string deviceId);

    /// <summary>
    /// Gets the dto for client capabilities.
    /// </summary>
    /// <param name="capabilities">The client capabilities.</param>
    /// <returns><see cref="ClientCapabilitiesDto"/> of the device.</returns>
    ClientCapabilitiesDto ToClientCapabilitiesDto(ClientCapabilities capabilities);
}
