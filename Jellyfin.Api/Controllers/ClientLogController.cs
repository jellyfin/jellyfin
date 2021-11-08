using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.ClientLogDtos;
using MediaBrowser.Controller.ClientEvent;
using MediaBrowser.Controller.Configuration;
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
        private const int MaxDocumentSize = 1_000_000;
        private readonly IClientEventLogger _clientEventLogger;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientLogController"/> class.
        /// </summary>
        /// <param name="clientEventLogger">Instance of the <see cref="IClientEventLogger"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public ClientLogController(
            IClientEventLogger clientEventLogger,
            IServerConfigurationManager serverConfigurationManager)
        {
            _clientEventLogger = clientEventLogger;
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <summary>
        /// Post event from client.
        /// </summary>
        /// <param name="clientLogEventDto">The client log dto.</param>
        /// <response code="204">Event logged.</response>
        /// <response code="403">Event logging disabled.</response>
        /// <returns>Submission status.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult LogEvent([FromBody] ClientLogEventDto clientLogEventDto)
        {
            if (!_serverConfigurationManager.Configuration.AllowClientLogUpload)
            {
                return Forbid();
            }

            var (clientName, clientVersion, userId, deviceId) = GetRequestInformation();
            Log(clientLogEventDto, userId, clientName, clientVersion, deviceId);
            return NoContent();
        }

        /// <summary>
        /// Bulk post events from client.
        /// </summary>
        /// <param name="clientLogEventDtos">The list of client log dtos.</param>
        /// <response code="204">All events logged.</response>
        /// <response code="403">Event logging disabled.</response>
        /// <returns>Submission status.</returns>
        [HttpPost("Bulk")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult LogEvents([FromBody] ClientLogEventDto[] clientLogEventDtos)
        {
            if (!_serverConfigurationManager.Configuration.AllowClientLogUpload)
            {
                return Forbid();
            }

            var (clientName, clientVersion, userId, deviceId) = GetRequestInformation();
            foreach (var dto in clientLogEventDtos)
            {
                Log(dto, userId, clientName, clientVersion, deviceId);
            }

            return NoContent();
        }

        /// <summary>
        /// Upload a document.
        /// </summary>
        /// <response code="200">Document saved.</response>
        /// <response code="403">Event logging disabled.</response>
        /// <response code="413">Upload size too large.</response>
        /// <returns>Create response.</returns>
        [HttpPost("Document")]
        [ProducesResponseType(typeof(ClientLogDocumentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [AcceptsFile(MediaTypeNames.Text.Plain)]
        [RequestSizeLimit(MaxDocumentSize)]
        public async Task<ActionResult<ClientLogDocumentResponseDto>> LogFile()
        {
            if (!_serverConfigurationManager.Configuration.AllowClientLogUpload)
            {
                return Forbid();
            }

            if (Request.ContentLength > MaxDocumentSize)
            {
                // Manually validate to return proper status code.
                return StatusCode(StatusCodes.Status413PayloadTooLarge, $"Payload must be less than {MaxDocumentSize:N0} bytes");
            }

            var (clientName, clientVersion, _, _) = GetRequestInformation();
            var fileName = await _clientEventLogger.WriteDocumentAsync(clientName, clientVersion, Request.Body)
                .ConfigureAwait(false);
            return Ok(new ClientLogDocumentResponseDto(fileName));
        }

        private void Log(
            ClientLogEventDto dto,
            Guid userId,
            string clientName,
            string clientVersion,
            string deviceId)
        {
            _clientEventLogger.Log(new ClientLogEvent(
                dto.Timestamp,
                dto.Level,
                userId,
                clientName,
                clientVersion,
                deviceId,
                dto.Message));
        }

        private (string ClientName, string ClientVersion, Guid UserId, string DeviceId) GetRequestInformation()
        {
            var clientName = ClaimHelpers.GetClient(HttpContext.User) ?? "unknown-client";
            var clientVersion = ClaimHelpers.GetIsApiKey(HttpContext.User)
                ? "apikey"
                : ClaimHelpers.GetVersion(HttpContext.User) ?? "unknown-version";
            var userId = ClaimHelpers.GetUserId(HttpContext.User) ?? Guid.Empty;
            var deviceId = ClaimHelpers.GetDeviceId(HttpContext.User) ?? "unknown-device-id";

            return (clientName, clientVersion, userId, deviceId);
        }
    }
}
