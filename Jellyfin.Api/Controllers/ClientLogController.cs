using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.ClientLogDtos;
using MediaBrowser.Controller.ClientEvent;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
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
        private readonly IAuthorizationContext _authorizationContext;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientLogController"/> class.
        /// </summary>
        /// <param name="clientEventLogger">Instance of the <see cref="IClientEventLogger"/> interface.</param>
        /// <param name="authorizationContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public ClientLogController(
            IClientEventLogger clientEventLogger,
            IAuthorizationContext authorizationContext,
            IServerConfigurationManager serverConfigurationManager)
        {
            _clientEventLogger = clientEventLogger;
            _authorizationContext = authorizationContext;
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
        public async Task<ActionResult> LogEvent([FromBody] ClientLogEventDto clientLogEventDto)
        {
            if (!_serverConfigurationManager.Configuration.AllowClientLogUpload)
            {
                return Forbid();
            }

            var authorizationInfo = await _authorizationContext.GetAuthorizationInfo(Request)
                .ConfigureAwait(false);

            Log(clientLogEventDto, authorizationInfo);
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
        public async Task<ActionResult> LogEvents([FromBody] ClientLogEventDto[] clientLogEventDtos)
        {
            if (!_serverConfigurationManager.Configuration.AllowClientLogUpload)
            {
                return Forbid();
            }

            var authorizationInfo = await _authorizationContext.GetAuthorizationInfo(Request)
                .ConfigureAwait(false);

            foreach (var dto in clientLogEventDtos)
            {
                Log(dto, authorizationInfo);
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

            var authorizationInfo = await _authorizationContext.GetAuthorizationInfo(Request)
                .ConfigureAwait(false);

            var fileName = await _clientEventLogger.WriteDocumentAsync(authorizationInfo, Request.Body)
                .ConfigureAwait(false);
            return Ok(new ClientLogDocumentResponseDto(fileName));
        }

        private void Log(ClientLogEventDto dto, AuthorizationInfo authorizationInfo)
        {
            _clientEventLogger.Log(new ClientLogEvent(
                dto.Timestamp,
                dto.Level,
                authorizationInfo.UserId,
                authorizationInfo.Client,
                authorizationInfo.Version,
                authorizationInfo.DeviceId,
                dto.Message));
        }
    }
}
