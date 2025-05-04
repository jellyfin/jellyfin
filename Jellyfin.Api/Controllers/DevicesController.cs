using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Dtos;
using Jellyfin.Data.Queries;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Devices Controller.
/// </summary>
[Authorize(Policy = Policies.RequiresElevation)]
public class DevicesController : BaseJellyfinApiController
{
    private readonly IDeviceManager _deviceManager;
    private readonly ISessionManager _sessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevicesController"/> class.
    /// </summary>
    /// <param name="deviceManager">Instance of <see cref="IDeviceManager"/> interface.</param>
    /// <param name="sessionManager">Instance of <see cref="ISessionManager"/> interface.</param>
    public DevicesController(
        IDeviceManager deviceManager,
        ISessionManager sessionManager)
    {
        _deviceManager = deviceManager;
        _sessionManager = sessionManager;
    }

    /// <summary>
    /// Get Devices.
    /// </summary>
    /// <param name="userId">Gets or sets the user identifier.</param>
    /// <response code="200">Devices retrieved.</response>
    /// <returns>An <see cref="OkResult"/> containing the list of devices.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<DeviceInfoDto>> GetDevices([FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        return _deviceManager.GetDevicesForUser(userId);
    }

    /// <summary>
    /// Get info for a device.
    /// </summary>
    /// <param name="id">Device Id.</param>
    /// <response code="200">Device info retrieved.</response>
    /// <response code="404">Device not found.</response>
    /// <returns>An <see cref="OkResult"/> containing the device info on success, or a <see cref="NotFoundResult"/> if the device could not be found.</returns>
    [HttpGet("Info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DeviceInfoDto> GetDeviceInfo([FromQuery, Required] string id)
    {
        var deviceInfo = _deviceManager.GetDevice(id);
        if (deviceInfo is null)
        {
            return NotFound();
        }

        return deviceInfo;
    }

    /// <summary>
    /// Get options for a device.
    /// </summary>
    /// <param name="id">Device Id.</param>
    /// <response code="200">Device options retrieved.</response>
    /// <response code="404">Device not found.</response>
    /// <returns>An <see cref="OkResult"/> containing the device info on success, or a <see cref="NotFoundResult"/> if the device could not be found.</returns>
    [HttpGet("Options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DeviceOptionsDto> GetDeviceOptions([FromQuery, Required] string id)
    {
        var deviceInfo = _deviceManager.GetDeviceOptions(id);
        if (deviceInfo is null)
        {
            return NotFound();
        }

        return deviceInfo;
    }

    /// <summary>
    /// Update device options.
    /// </summary>
    /// <param name="id">Device Id.</param>
    /// <param name="deviceOptions">Device Options.</param>
    /// <response code="204">Device options updated.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Options")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UpdateDeviceOptions(
        [FromQuery, Required] string id,
        [FromBody, Required] DeviceOptionsDto deviceOptions)
    {
        await _deviceManager.UpdateDeviceOptions(id, deviceOptions.CustomName).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Deletes a device.
    /// </summary>
    /// <param name="id">Device Id.</param>
    /// <response code="204">Device deleted.</response>
    /// <response code="404">Device not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the device could not be found.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDevice([FromQuery, Required] string id)
    {
        var existingDevice = _deviceManager.GetDevice(id);
        if (existingDevice is null)
        {
            return NotFound();
        }

        var sessions = _deviceManager.GetDevices(new DeviceQuery { DeviceId = id });

        foreach (var session in sessions.Items)
        {
            await _sessionManager.Logout(session).ConfigureAwait(false);
        }

        return NoContent();
    }
}
