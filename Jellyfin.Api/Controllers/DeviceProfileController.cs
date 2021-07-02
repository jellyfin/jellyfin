using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Api.Constants;
using Jellyfin.DeviceProfiles;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api
{
    /// <summary>
    /// Dlna Controller.
    /// </summary>
    [Authorize(Policy = Policies.RequiresElevation)]
    public class DeviceProfileController : BaseJellyfinApiController
    {
        private readonly IDeviceProfileManager _profileManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProfileController"/> class.
        /// </summary>
        /// <param name="profileManager">Instance of the <see cref="IDeviceProfileManager"/> interface.</param>
        public DeviceProfileController(IDeviceProfileManager profileManager) => _profileManager = profileManager;

        /// <summary>
        /// Gets the default profile.
        /// </summary>
        /// <response code="200">Default device profile returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the default profile.</returns>
        [HttpGet("Profiles/Default")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<DeviceProfile> GetDefaultProfile()
        {
            return _profileManager.DefaultProfile();
        }

        /// <summary>
        /// Get profile infos.
        /// </summary>
        /// <response code="200">Device profile infos returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the device profile infos.</returns>
        [HttpGet("ProfileInfos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<DeviceProfileInfo>> GetProfileInfos()
        {
            return Ok(_profileManager.GetProfileInfos());
        }

        /// <summary>
        /// Reloads user template profiles from the disk.
        /// </summary>
        /// <response code="200">Device profile loaded.</response>
        /// <returns>An <see cref="OkResult"/>.</returns>
        [HttpGet("Refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult ReloadUserTemplates()
        {
            _profileManager.ReloadUserTemplates();
            return Ok();
        }

        /// <summary>
        /// Gets a single profile.
        /// </summary>
        /// <param name="profileId">Profile Id.</param>
        /// <response code="200">Device profile returned.</response>
        /// <response code="404">Device profile not found.</response>
        /// <returns>An <see cref="OkResult"/> containing the profile on success, or a <see cref="NotFoundResult"/> if device profile not found.</returns>
        [HttpGet("Profiles/{profileId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<DeviceProfile> GetProfile([FromRoute, Required] Guid profileId)
        {
            var profile = _profileManager.GetProfile(profileId, false);
            if (profile == null)
            {
                return NotFound();
            }

            return profile;
        }

        /// <summary>
        /// Deletes a profile.
        /// </summary>
        /// <param name="profileId">Profile id.</param>
        /// <response code="204">Device profile deleted.</response>
        /// <response code="404">Device profile not found.</response>
        /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if profile not found.</returns>
        [HttpDelete("Profiles/{profileId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteProfile([FromRoute, Required] Guid profileId)
        {
            return _profileManager.DeleteProfile(profileId) ? NoContent() : NotFound();
        }

        /// <summary>
        /// Creates a profile.
        /// </summary>
        /// <param name="deviceProfile">Device profile.</param>
        /// <response code="204">Device profile created.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("Profiles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult CreateProfile([FromBody] DeviceProfile deviceProfile)
        {
            _profileManager.AddProfile(deviceProfile);
            return NoContent();
        }

        /// <summary>
        /// Updates a profile.
        /// </summary>
        /// <param name="profileId">Profile id.</param>
        /// <param name="deviceProfile">Device profile.</param>
        /// <response code="204">Device profile updated.</response>
        /// <response code="404">Device profile not found.</response>
        /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if profile not found.</returns>
        [HttpPost("Profiles/{profileId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateProfile([FromRoute, Required] Guid profileId, [FromBody] DeviceProfile deviceProfile)
        {
            return _profileManager.UpdateProfile(profileId, deviceProfile) ? NoContent() : NotFound();
        }
    }
}
