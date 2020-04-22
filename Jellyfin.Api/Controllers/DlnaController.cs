#nullable enable

using System.Collections.Generic;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Dlna Controller.
    /// </summary>
    [Authenticated(Roles = "Admin")]
    public class DlnaController : BaseJellyfinApiController
    {
        private readonly IDlnaManager _dlnaManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaController"/> class.
        /// </summary>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        public DlnaController(IDlnaManager dlnaManager)
        {
            _dlnaManager = dlnaManager;
        }

        /// <summary>
        /// Get profile infos.
        /// </summary>
        /// <returns>Profile infos.</returns>
        [HttpGet("ProfileInfos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<DeviceProfileInfo> GetProfileInfos()
        {
            return _dlnaManager.GetProfileInfos();
        }

        /// <summary>
        /// Gets the default profile.
        /// </summary>
        /// <returns>Default profile.</returns>
        [HttpGet("Profiles/Default")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<DeviceProfileInfo> GetDefaultProfile()
        {
            return Ok(_dlnaManager.GetDefaultProfile());
        }

        /// <summary>
        /// Gets a single profile.
        /// </summary>
        /// <param name="id">Profile Id.</param>
        /// <returns>Profile.</returns>
        [HttpGet("Profiles/{Id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<DeviceProfileInfo> GetProfile([FromRoute] string id)
        {
            var profile = _dlnaManager.GetProfile(id);
            if (profile == null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        /// <summary>
        /// Deletes a profile.
        /// </summary>
        /// <param name="id">Profile id.</param>
        /// <returns>Status.</returns>
        [HttpDelete("Profiles/{Id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteProfile([FromRoute] string id)
        {
            var existingDeviceProfile = _dlnaManager.GetProfile(id);
            if (existingDeviceProfile == null)
            {
                return NotFound();
            }

            _dlnaManager.DeleteProfile(id);
            return Ok();
        }

        /// <summary>
        /// Creates a profile.
        /// </summary>
        /// <param name="deviceProfile">Device profile.</param>
        /// <returns>Status.</returns>
        [HttpPost("Profiles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CreateProfile([FromBody] DeviceProfile deviceProfile)
        {
            _dlnaManager.CreateProfile(deviceProfile);
            return Ok();
        }

        /// <summary>
        /// Updates a profile.
        /// </summary>
        /// <param name="id">Profile id.</param>
        /// <param name="deviceProfile">Device profile.</param>
        /// <returns>Status.</returns>
        [HttpPost("Profiles/{Id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateProfile([FromRoute] string id, [FromBody] DeviceProfile deviceProfile)
        {
            var existingDeviceProfile = _dlnaManager.GetProfile(id);
            if (existingDeviceProfile == null)
            {
                return NotFound();
            }

            _dlnaManager.UpdateProfile(deviceProfile);
            return Ok();
        }
    }
}
