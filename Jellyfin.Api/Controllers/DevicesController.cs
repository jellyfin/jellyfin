using System;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Devices Controller.
    /// </summary>
    [Authorize(Policy = Policies.RequiresElevation)]
    public class DevicesController : BaseJellyfinApiController
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DevicesController"/> class.
        /// </summary>
        /// <param name="deviceManager">Instance of <see cref="IDeviceManager"/> interface.</param>
        /// <param name="authenticationRepository">Instance of <see cref="IAuthenticationRepository"/> interface.</param>
        /// <param name="sessionManager">Instance of <see cref="ISessionManager"/> interface.</param>
        public DevicesController(
            IDeviceManager deviceManager,
            IAuthenticationRepository authenticationRepository,
            ISessionManager sessionManager)
        {
            _deviceManager = deviceManager;
            _authenticationRepository = authenticationRepository;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Get Devices.
        /// </summary>
        /// <param name="supportsSync">Gets or sets a value indicating whether [supports synchronize].</param>
        /// <param name="userId">Gets or sets the user identifier.</param>
        /// <response code="200">Devices retrieved.</response>
        /// <returns>An <see cref="OkResult"/> containing the list of devices.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<DeviceInfo>> GetDevices([FromQuery] bool? supportsSync, [FromQuery] Guid? userId)
        {
            var deviceQuery = new DeviceQuery { SupportsSync = supportsSync, UserId = userId ?? Guid.Empty };
            return _deviceManager.GetDevices(deviceQuery);
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
        public ActionResult<DeviceInfo> GetDeviceInfo([FromQuery, Required] string id)
        {
            var deviceInfo = _deviceManager.GetDevice(id);
            if (deviceInfo == null)
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
        public ActionResult<DeviceOptions> GetDeviceOptions([FromQuery, Required] string id)
        {
            var deviceInfo = _deviceManager.GetDeviceOptions(id);
            if (deviceInfo == null)
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
        /// <response code="404">Device not found.</response>
        /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the device could not be found.</returns>
        [HttpPost("Options")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateDeviceOptions(
            [FromQuery, Required] string id,
            [FromBody, Required] DeviceOptions deviceOptions)
        {
            var existingDeviceOptions = _deviceManager.GetDeviceOptions(id);
            if (existingDeviceOptions == null)
            {
                return NotFound();
            }

            _deviceManager.UpdateDeviceOptions(id, deviceOptions);
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
        public ActionResult DeleteDevice([FromQuery, Required] string id)
        {
            var existingDevice = _deviceManager.GetDevice(id);
            if (existingDevice == null)
            {
                return NotFound();
            }

            var sessions = _authenticationRepository.Get(new AuthenticationInfoQuery { DeviceId = id }).Items;

            foreach (var session in sessions)
            {
                _sessionManager.Logout(session);
            }

            return NoContent();
        }
    }
}
