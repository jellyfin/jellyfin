using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Emby.Dlna;
using Emby.Dlna.Main;
using Jellyfin.Api.Attributes;
using MediaBrowser.Controller.Dlna;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Dlna Server Controller.
    /// </summary>
    [Route("Dlna")]
    public class DlnaServerController : BaseJellyfinApiController
    {
        private readonly IDlnaManager _dlnaManager;
        private readonly IContentDirectory _contentDirectory;
        private readonly IConnectionManager _connectionManager;
        private readonly IMediaReceiverRegistrar _mediaReceiverRegistrar;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaServerController"/> class.
        /// </summary>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        public DlnaServerController(IDlnaManager dlnaManager)
        {
            _dlnaManager = dlnaManager;
            _contentDirectory = DlnaEntryPoint.Current.ContentDirectory;
            _connectionManager = DlnaEntryPoint.Current.ConnectionManager;
            _mediaReceiverRegistrar = DlnaEntryPoint.Current.MediaReceiverRegistrar;
        }

        /// <summary>
        /// Get Description Xml.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Description xml returned.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>An <see cref="OkResult"/> containing the description xml.</returns>
        [HttpGet("{serverId}/description")]
        [HttpGet("{serverId}/description.xml", Name = "GetDescriptionXml_2")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public ActionResult GetDescriptionXml([FromRoute, Required] string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                var url = GetAbsoluteUri();
                var serverAddress = url.Substring(0, url.IndexOf("/dlna/", StringComparison.OrdinalIgnoreCase));
                var xml = _dlnaManager.GetServerDescriptionXml(Request.Headers, serverId, serverAddress);
                return Ok(xml);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Gets Dlna content directory xml.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Dlna content directory returned.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>An <see cref="OkResult"/> containing the dlna content directory xml.</returns>
        [HttpGet("{serverId}/ContentDirectory")]
        [HttpGet("{serverId}/ContentDirectory/ContentDirectory", Name = "GetContentDirectory_2")]
        [HttpGet("{serverId}/ContentDirectory/ContentDirectory.xml", Name = "GetContentDirectory_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "serverId", Justification = "Required for DLNA")]
        public ActionResult GetContentDirectory([FromRoute, Required] string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return Ok(_contentDirectory.GetServiceXml());
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Gets Dlna media receiver registrar xml.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Dlna media receiver registrar xml returned.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Dlna media receiver registrar xml.</returns>
        [HttpGet("{serverId}/MediaReceiverRegistrar")]
        [HttpGet("{serverId}/MediaReceiverRegistrar/MediaReceiverRegistrar", Name = "GetMediaReceiverRegistrar_2")]
        [HttpGet("{serverId}/MediaReceiverRegistrar/MediaReceiverRegistrar.xml", Name = "GetMediaReceiverRegistrar_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "serverId", Justification = "Required for DLNA")]
        public ActionResult GetMediaReceiverRegistrar([FromRoute, Required] string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return Ok(_mediaReceiverRegistrar.GetServiceXml());
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Gets Dlna media receiver registrar xml.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Dlna media receiver registrar xml returned.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Dlna media receiver registrar xml.</returns>
        [HttpGet("{serverId}/ConnectionManager")]
        [HttpGet("{serverId}/ConnectionManager/ConnectionManager", Name = "GetConnectionManager_2")]
        [HttpGet("{serverId}/ConnectionManager/ConnectionManager.xml", Name = "GetConnectionManager_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "serverId", Justification = "Required for DLNA")]
        public ActionResult GetConnectionManager([FromRoute, Required] string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return Ok(_connectionManager.GetServiceXml());
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Process a content directory control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Request processed.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/ContentDirectory/Control")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult<ControlResponse>> ProcessContentDirectoryControlRequest([FromRoute, Required] string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return await ProcessControlRequestInternalAsync(serverId, Request.Body, _contentDirectory).ConfigureAwait(false);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Process a connection manager control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Request processed.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/ConnectionManager/Control")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult<ControlResponse>> ProcessConnectionManagerControlRequest([FromRoute, Required] string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return await ProcessControlRequestInternalAsync(serverId, Request.Body, _connectionManager).ConfigureAwait(false);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Process a media receiver registrar control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Request processed.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/MediaReceiverRegistrar/Control")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult<ControlResponse>> ProcessMediaReceiverRegistrarControlRequest([FromRoute, Required] string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return await ProcessControlRequestInternalAsync(serverId, Request.Body, _mediaReceiverRegistrar).ConfigureAwait(false);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Request processed.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{serverId}/MediaReceiverRegistrar/Events")]
        [HttpUnsubscribe("{serverId}/MediaReceiverRegistrar/Events")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ignore in openapi docs
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "serverId", Justification = "Required for DLNA")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public ActionResult<EventSubscriptionResponse> ProcessMediaReceiverRegistrarEventRequest(string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return ProcessEventRequest(_mediaReceiverRegistrar);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Request processed.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{serverId}/ContentDirectory/Events")]
        [HttpUnsubscribe("{serverId}/ContentDirectory/Events")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ignore in openapi docs
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "serverId", Justification = "Required for DLNA")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public ActionResult<EventSubscriptionResponse> ProcessContentDirectoryEventRequest(string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return ProcessEventRequest(_contentDirectory);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Request processed.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{serverId}/ConnectionManager/Events")]
        [HttpUnsubscribe("{serverId}/ConnectionManager/Events")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ignore in openapi docs
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "serverId", Justification = "Required for DLNA")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public ActionResult<EventSubscriptionResponse> ProcessConnectionManagerEventRequest(string serverId)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return ProcessEventRequest(_connectionManager);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Gets a server icon.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <param name="fileName">The icon filename.</param>
        /// <response code="200">Request processed.</response>
        /// <response code="404">Not Found.</response>
        /// <response code="503">DLNA is disabled.</response>
        /// <returns>Icon stream.</returns>
        [HttpGet("{serverId}/icons/{fileName}")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "serverId", Justification = "Required for DLNA")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesImageFile]
        public ActionResult GetIconId([FromRoute, Required] string serverId, [FromRoute, Required] string fileName)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return GetIconInternal(fileName);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        /// <summary>
        /// Gets a server icon.
        /// </summary>
        /// <param name="fileName">The icon filename.</param>
        /// <returns>Icon stream.</returns>
        /// <response code="200">Request processed.</response>
        /// <response code="404">Not Found.</response>
        /// <response code="503">DLNA is disabled.</response>
        [HttpGet("icons/{fileName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesImageFile]
        public ActionResult GetIcon([FromRoute, Required] string fileName)
        {
            if (DlnaEntryPoint.Enabled)
            {
                return GetIconInternal(fileName);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        private ActionResult GetIconInternal(string fileName)
        {
            var icon = _dlnaManager.GetIcon(fileName);
            if (icon == null)
            {
                return NotFound();
            }

            var contentType = "image/" + Path.GetExtension(fileName)
                .TrimStart('.')
                .ToLowerInvariant();

            return File(icon.Stream, contentType);
        }

        private string GetAbsoluteUri()
        {
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        }

        private Task<ControlResponse> ProcessControlRequestInternalAsync(string id, Stream requestStream, IUpnpService service)
        {
            return service.ProcessControlRequestAsync(new ControlRequest(Request.Headers)
            {
                InputXml = requestStream,
                TargetServerUuId = id,
                RequestedUrl = GetAbsoluteUri()
            });
        }

        private EventSubscriptionResponse ProcessEventRequest(IDlnaEventManager dlnaEventManager)
        {
            var subscriptionId = Request.Headers["SID"];
            if (string.Equals(Request.Method, "subscribe", StringComparison.OrdinalIgnoreCase))
            {
                var notificationType = Request.Headers["NT"];
                var callback = Request.Headers["CALLBACK"];
                var timeoutString = Request.Headers["TIMEOUT"];

                if (string.IsNullOrEmpty(notificationType))
                {
                    return dlnaEventManager.RenewEventSubscription(
                        subscriptionId,
                        notificationType,
                        timeoutString,
                        callback);
                }

                return dlnaEventManager.CreateEventSubscription(notificationType, timeoutString, callback);
            }

            return dlnaEventManager.CancelEventSubscription(subscriptionId);
        }
    }
}
