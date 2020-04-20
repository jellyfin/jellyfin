#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Devices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Devices Controller.
    /// </summary>
    [Authenticated]
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
        /// <param name="supportsSync">/// Gets or sets a value indicating whether [supports synchronize].</param>
        /// <param name="userId">/// Gets or sets the user identifier.</param>
        /// <returns>Device Infos.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(DeviceInfo[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetDevices([FromQuery] bool? supportsSync, [FromQuery] Guid? userId)
        {
            try
            {
                var deviceQuery = new DeviceQuery { SupportsSync = supportsSync, UserId = userId ?? Guid.Empty };
                var devices = _deviceManager.GetDevices(deviceQuery);
                return Ok(devices);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Get info for a device.
        /// </summary>
        /// <param name="id">Device Id.</param>
        /// <returns>Device Info.</returns>
        [HttpGet("Info")]
        [ProducesResponseType(typeof(DeviceInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetDeviceInfo([FromQuery, BindRequired] string id)
        {
            try
            {
                var deviceInfo = _deviceManager.GetDevice(id);
                if (deviceInfo == null)
                {
                    return NotFound();
                }

                return Ok(deviceInfo);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Get options for a device.
        /// </summary>
        /// <param name="id">Device Id.</param>
        /// <returns>Device Info.</returns>
        [HttpGet("Options")]
        [ProducesResponseType(typeof(DeviceOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetDeviceOptions([FromQuery, BindRequired] string id)
        {
            try
            {
                var deviceInfo = _deviceManager.GetDeviceOptions(id);
                if (deviceInfo == null)
                {
                    return NotFound();
                }

                return Ok(deviceInfo);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Update device options.
        /// </summary>
        /// <param name="id">Device Id.</param>
        /// <param name="deviceOptions">Device Options.</param>
        /// <returns>Status.</returns>
        [HttpPost("Options")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateDeviceOptions(
            [FromQuery, BindRequired] string id,
            [FromBody, BindRequired] DeviceOptions deviceOptions)
        {
            try
            {
                var existingDeviceOptions = _deviceManager.GetDeviceOptions(id);
                if (existingDeviceOptions == null)
                {
                    return NotFound();
                }

                _deviceManager.UpdateDeviceOptions(id, deviceOptions);
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Deletes a device.
        /// </summary>
        /// <param name="id">Device Id.</param>
        /// <returns>Status.</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult DeleteDevice([FromQuery, BindRequired] string id)
        {
            try
            {
                var sessions = _authenticationRepository.Get(new AuthenticationInfoQuery { DeviceId = id }).Items;

                foreach (var session in sessions)
                {
                    _sessionManager.Logout(session);
                }

                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Gets camera upload history for a device.
        /// </summary>
        /// <param name="id">Device Id.</param>
        /// <returns>Content Upload History.</returns>
        [HttpGet("CameraUploads")]
        [ProducesResponseType(typeof(ContentUploadHistory), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetCameraUploads([FromQuery, BindRequired] string id)
        {
            try
            {
                var uploadHistory = _deviceManager.GetCameraUploadHistory(id);
                return Ok(uploadHistory);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Uploads content.
        /// </summary>
        /// <param name="deviceId">Device Id.</param>
        /// <param name="album">Album.</param>
        /// <param name="name">Name.</param>
        /// <param name="id">Id.</param>
        /// <returns>Status.</returns>
        [HttpPost("CameraUploads")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostCameraUploadAsync(
            [FromQuery, BindRequired] string deviceId,
            [FromQuery, BindRequired] string album,
            [FromQuery, BindRequired] string name,
            [FromQuery, BindRequired] string id)
        {
            try
            {
                Stream fileStream;
                string contentType;

                if (Request.HasFormContentType)
                {
                    if (Request.Form.Files.Any())
                    {
                        fileStream = Request.Form.Files[0].OpenReadStream();
                        contentType = Request.Form.Files[0].ContentType;
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                else
                {
                    fileStream = Request.Body;
                    contentType = Request.ContentType;
                }

                await _deviceManager.AcceptCameraUpload(
                    deviceId,
                    fileStream,
                    new LocalFileInfo
                    {
                        MimeType = contentType,
                        Album = album,
                        Name = name,
                        Id = id
                    }).ConfigureAwait(false);

                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
