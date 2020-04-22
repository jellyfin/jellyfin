#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Emby.Dlna;
using Emby.Dlna.Main;
using Jellyfin.Api.Attributes;
using MediaBrowser.Controller.Dlna;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA1801

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Dlna Server Controller.
    /// </summary>
    [Route("Dlna")]
    public class DlnaServerController : BaseJellyfinApiController
    {
        private const string XMLContentType = "text/xml; charset=UTF-8";

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
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Description Xml.</returns>
        [HttpGet("{Uuid}/description.xml")]
        [HttpGet("{Uuid}/description")]
        [Produces(XMLContentType)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetDescriptionXml([FromRoute] string uuid)
        {
            var url = GetAbsoluteUri();
            var serverAddress = url.Substring(0, url.IndexOf("/dlna/", StringComparison.OrdinalIgnoreCase));
            var xml = _dlnaManager.GetServerDescriptionXml(Request.Headers, uuid, serverAddress);

            // TODO GetStaticResult doesn't do anything special?
            /*
            var cacheLength = TimeSpan.FromDays(1);
            var cacheKey = Request.Path.Value.GetMD5();
            var bytes = Encoding.UTF8.GetBytes(xml);
            */
            return Ok(xml);
        }

        /// <summary>
        /// Gets Dlna content directory xml.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Dlna content directory xml.</returns>
        [HttpGet("{Uuid}/ContentDirectory/ContentDirectory.xml")]
        [HttpGet("{Uuid}/ContentDirectory/ContentDirectory")]
        [Produces(XMLContentType)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetContentDirectory([FromRoute] string uuid)
        {
            return Ok(_contentDirectory.GetServiceXml());
        }

        /// <summary>
        /// Gets Dlna media receiver registrar xml.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Dlna media receiver registrar xml.</returns>
        [HttpGet("{Uuid}/MediaReceiverRegistrar/MediaReceiverRegistrar.xml")]
        [HttpGet("{Uuid}/MediaReceiverRegistrar/MediaReceiverRegistrar")]
        [Produces(XMLContentType)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetMediaReceiverRegistrar([FromRoute] string uuid)
        {
            return Ok(_mediaReceiverRegistrar.GetServiceXml());
        }

        /// <summary>
        /// Gets Dlna media receiver registrar xml.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Dlna media receiver registrar xml.</returns>
        [HttpGet("{Uuid}/ConnectionManager/ConnectionManager.xml")]
        [HttpGet("{Uuid}/ConnectionManager/ConnectionManager")]
        [Produces(XMLContentType)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetConnectionManager([FromRoute] string uuid)
        {
            return Ok(_connectionManager.GetServiceXml());
        }

        /// <summary>
        /// Process a content directory control request.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{Uuid}/ContentDirectory/Control")]
        public async Task<ActionResult<ControlResponse>> ProcessContentDirectoryControlRequest([FromRoute] string uuid)
        {
            var response = await PostAsync(uuid, Request.Body, _contentDirectory).ConfigureAwait(false);
            return Ok(response);
        }

        /// <summary>
        /// Process a connection manager control request.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{Uuid}/ConnectionManager/Control")]
        public async Task<ActionResult<ControlResponse>> ProcessConnectionManagerControlRequest([FromRoute] string uuid)
        {
            var response = await PostAsync(uuid, Request.Body, _connectionManager).ConfigureAwait(false);
            return Ok(response);
        }

        /// <summary>
        /// Process a media receiver registrar control request.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{Uuid}/MediaReceiverRegistrar/Control")]
        public async Task<ActionResult<ControlResponse>> ProcessMediaReceiverRegistrarControlRequest([FromRoute] string uuid)
        {
            var response = await PostAsync(uuid, Request.Body, _mediaReceiverRegistrar).ConfigureAwait(false);
            return Ok(response);
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{Uuid}/MediaReceiverRegistrar/Events")]
        [HttpUnsubscribe("{Uuid}/MediaReceiverRegistrar/Events")]
        public ActionResult<EventSubscriptionResponse> ProcessMediaReceiverRegistrarEventRequest(string uuid)
        {
            return Ok(ProcessEventRequest(_mediaReceiverRegistrar));
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{Uuid}/ContentDirectory/Events")]
        [HttpUnsubscribe("{Uuid}/ContentDirectory/Events")]
        public ActionResult<EventSubscriptionResponse> ProcessContentDirectoryEventRequest(string uuid)
        {
            return Ok(ProcessEventRequest(_contentDirectory));
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{Uuid}/ConnectionManager/Events")]
        [HttpUnsubscribe("{Uuid}/ConnectionManager/Events")]
        public ActionResult<EventSubscriptionResponse> ProcessConnectionManagerEventRequest(string uuid)
        {
            return Ok(ProcessEventRequest(_connectionManager));
        }

        /// <summary>
        /// Gets a server icon.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <param name="fileName">The icon filename.</param>
        /// <returns>Icon stream.</returns>
        [HttpGet("{Uuid}/icons/{Filename}")]
        public ActionResult<FileStreamResult> GetIconId([FromRoute] string uuid, [FromRoute] string fileName)
        {
            return GetIcon(fileName);
        }

        /// <summary>
        /// Gets a server icon.
        /// </summary>
        /// <param name="uuid">Server UUID.</param>
        /// <param name="fileName">The icon filename.</param>
        /// <returns>Icon stream.</returns>
        [HttpGet("icons/{Filename}")]
        public ActionResult<FileStreamResult> GetIcon([FromQuery] string uuid, [FromRoute] string fileName)
        {
            return GetIcon(fileName);
        }

        private ActionResult<FileStreamResult> GetIcon(string fileName)
        {
            var icon = _dlnaManager.GetIcon(fileName);
            if (icon == null)
            {
                return NotFound();
            }

            var contentType = "image/" + Path.GetExtension(fileName)
                .TrimStart('.')
                .ToLowerInvariant();

            return new FileStreamResult(icon.Stream, contentType);
        }

        private string GetAbsoluteUri()
        {
            return $"{Request.Scheme}://{Request.Host}{Request.Path}";
        }

        private Task<ControlResponse> PostAsync(string id, Stream requestStream, IUpnpService service)
        {
            return service.ProcessControlRequestAsync(new ControlRequest
            {
                Headers = Request.Headers,
                InputXml = requestStream,
                TargetServerUuId = id,
                RequestedUrl = GetAbsoluteUri()
            });
        }

        private EventSubscriptionResponse ProcessEventRequest(IEventManager eventManager)
        {
            var subscriptionId = Request.Headers["SID"];
            if (string.Equals(Request.Method, "subscribe", StringComparison.OrdinalIgnoreCase))
            {
                var notificationType = Request.Headers["NT"];
                var callback = Request.Headers["CALLBACK"];
                var timeoutString = Request.Headers["TIMEOUT"];

                if (string.IsNullOrEmpty(notificationType))
                {
                    return eventManager.RenewEventSubscription(
                        subscriptionId,
                        notificationType,
                        timeoutString,
                        callback);
                }

                return eventManager.CreateEventSubscription(notificationType, timeoutString, callback);
            }

            return eventManager.CancelEventSubscription(subscriptionId);
        }
    }
}
