using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Display Preferences Controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class DisplayPreferencesController : BaseJellyfinApiController
    {
        private readonly IDisplayPreferencesRepository _displayPreferencesRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesController"/> class.
        /// </summary>
        /// <param name="displayPreferencesRepository">Instance of <see cref="IDisplayPreferencesRepository"/> interface.</param>
        public DisplayPreferencesController(IDisplayPreferencesRepository displayPreferencesRepository)
        {
            _displayPreferencesRepository = displayPreferencesRepository;
        }

        /// <summary>
        /// Get Display Preferences.
        /// </summary>
        /// <param name="displayPreferencesId">Display preferences id.</param>
        /// <param name="userId">User id.</param>
        /// <param name="client">Client.</param>
        /// <response code="200">Display preferences retrieved.</response>
        /// <returns>An <see cref="OkResult"/> containing the display preferences on success, or a <see cref="NotFoundResult"/> if the display preferences could not be found.</returns>
        [HttpGet("{displayPreferencesId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<DisplayPreferences> GetDisplayPreferences(
            [FromRoute] string displayPreferencesId,
            [FromQuery] [Required] string userId,
            [FromQuery] [Required] string client)
        {
            return _displayPreferencesRepository.GetDisplayPreferences(displayPreferencesId, userId, client);
        }

        /// <summary>
        /// Update Display Preferences.
        /// </summary>
        /// <param name="displayPreferencesId">Display preferences id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="client">Client.</param>
        /// <param name="displayPreferences">New Display Preferences object.</param>
        /// <response code="204">Display preferences updated.</response>
        /// <returns>An <see cref="NoContentResult"/> on success.</returns>
        [HttpPost("{displayPreferencesId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "displayPreferencesId", Justification = "Imported from ServiceStack")]
        public ActionResult UpdateDisplayPreferences(
            [FromRoute] string displayPreferencesId,
            [FromQuery, BindRequired] string userId,
            [FromQuery, BindRequired] string client,
            [FromBody, BindRequired] DisplayPreferences displayPreferences)
        {
            _displayPreferencesRepository.SaveDisplayPreferences(
                displayPreferences,
                userId,
                client,
                CancellationToken.None);

            return NoContent();
        }
    }
}
