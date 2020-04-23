#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Threading;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Display Preferences Controller.
    /// </summary>
    [Authenticated]
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
        /// <returns>Display Preferences.</returns>
        [HttpGet("{DisplayPreferencesId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<DisplayPreferences> GetDisplayPreferences(
            [FromRoute] string displayPreferencesId,
            [FromQuery] [Required] string userId,
            [FromQuery] [Required] string client)
        {
            var result = _displayPreferencesRepository.GetDisplayPreferences(displayPreferencesId, userId, client);
            if (result == null)
            {
                return NotFound();
            }

            return result;
        }

        /// <summary>
        /// Update Display Preferences.
        /// </summary>
        /// <param name="displayPreferencesId">Display preferences id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="client">Client.</param>
        /// <param name="displayPreferences">New Display Preferences object.</param>
        /// <returns>Status.</returns>
        [HttpPost("{DisplayPreferencesId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateDisplayPreferences(
            [FromRoute] string displayPreferencesId,
            [FromQuery, BindRequired] string userId,
            [FromQuery, BindRequired] string client,
            [FromBody, BindRequired] DisplayPreferences displayPreferences)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (displayPreferencesId == null)
            {
                // do nothing.
            }

            _displayPreferencesRepository.SaveDisplayPreferences(
                displayPreferences,
                userId,
                client,
                CancellationToken.None);

            return Ok();
        }
    }
}
