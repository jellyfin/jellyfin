using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.ClientLogDtos;
using MediaBrowser.Controller.ClientEvent;
using MediaBrowser.Model.ClientLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Client log controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class ClientLogController : BaseJellyfinApiController
    {
        private readonly IClientEventLogger _clientEventLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientLogController"/> class.
        /// </summary>
        /// <param name="clientEventLogger">Instance of the <see cref="IClientEventLogger"/> interface.</param>
        public ClientLogController(IClientEventLogger clientEventLogger)
        {
            _clientEventLogger = clientEventLogger;
        }

        /// <summary>
        /// Post event from client.
        /// </summary>
        /// <param name="clientLogEventDto">The client log dto.</param>
        /// <response code="204">Event logged.</response>
        /// <returns>Submission status.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult LogEvent([FromBody] ClientLogEventDto clientLogEventDto)
        {
            Log(clientLogEventDto);
            return NoContent();
        }

        /// <summary>
        /// Bulk post events from client.
        /// </summary>
        /// <param name="clientLogEventDtos">The list of client log dtos.</param>
        /// <response code="204">All events logged.</response>
        /// <returns>Submission status.</returns>
        [HttpPost("Bulk")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult LogEvents([FromBody] ClientLogEventDto[] clientLogEventDtos)
        {
            foreach (var dto in clientLogEventDtos)
            {
                Log(dto);
            }

            return NoContent();
        }

        private void Log(ClientLogEventDto dto)
        {
            _clientEventLogger.Log(new ClientLogEvent(
                dto.Timestamp,
                dto.Level,
                dto.UserId,
                dto.ClientName,
                dto.ClientVersion,
                dto.DeviceId,
                dto.Message));
        }
    }
}