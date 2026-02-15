using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.ClientLogDtos;
using MediaBrowser.Controller.ClientEvent;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Client log controller.
/// </summary>
[Authorize]
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

        var (clientName, clientVersion) = GetRequestInformation();
        var fileName = await _clientEventLogger.WriteDocumentAsync(clientName, clientVersion, Request.Body)
            .ConfigureAwait(false);
        return Ok(new ClientLogDocumentResponseDto(fileName));
    }

    private (string ClientName, string ClientVersion) GetRequestInformation()
    {
        var clientName = HttpContext.User.GetClient() ?? "unknown-client";
        var clientVersion = HttpContext.User.GetIsApiKey()
            ? "apikey"
            : HttpContext.User.GetVersion() ?? "unknown-version";

        return (clientName, clientVersion);
    }
}
