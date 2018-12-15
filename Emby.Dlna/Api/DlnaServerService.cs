using MediaBrowser.Controller.Dlna;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;
using MediaBrowser.Common.Extensions;
using System.Text;
using MediaBrowser.Controller.Net;
using System.Linq;
using Emby.Dlna.Main;

namespace Emby.Dlna.Api
{
    [Route("/Dlna/{UuId}/description.xml", "GET", Summary = "Gets dlna server info")]
    [Route("/Dlna/{UuId}/description", "GET", Summary = "Gets dlna server info")]
    public class GetDescriptionXml
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/contentdirectory/contentdirectory.xml", "GET", Summary = "Gets dlna content directory xml")]
    [Route("/Dlna/{UuId}/contentdirectory/contentdirectory", "GET", Summary = "Gets dlna content directory xml")]
    public class GetContentDirectory
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/connectionmanager/connectionmanager.xml", "GET", Summary = "Gets dlna connection manager xml")]
    [Route("/Dlna/{UuId}/connectionmanager/connectionmanager", "GET", Summary = "Gets dlna connection manager xml")]
    public class GetConnnectionManager
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/mediareceiverregistrar/mediareceiverregistrar.xml", "GET", Summary = "Gets dlna mediareceiverregistrar xml")]
    [Route("/Dlna/{UuId}/mediareceiverregistrar/mediareceiverregistrar", "GET", Summary = "Gets dlna mediareceiverregistrar xml")]
    public class GetMediaReceiverRegistrar
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/contentdirectory/control", "POST", Summary = "Processes a control request")]
    public class ProcessContentDirectoryControlRequest : IRequiresRequestStream
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/Dlna/{UuId}/connectionmanager/control", "POST", Summary = "Processes a control request")]
    public class ProcessConnectionManagerControlRequest : IRequiresRequestStream
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/Dlna/{UuId}/mediareceiverregistrar/control", "POST", Summary = "Processes a control request")]
    public class ProcessMediaReceiverRegistrarControlRequest : IRequiresRequestStream
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/Dlna/{UuId}/mediareceiverregistrar/events", "SUBSCRIBE", Summary = "Processes an event subscription request")]
    [Route("/Dlna/{UuId}/mediareceiverregistrar/events", "UNSUBSCRIBE", Summary = "Processes an event subscription request")]
    public class ProcessMediaReceiverRegistrarEventRequest
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "SUBSCRIBE,UNSUBSCRIBE")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/contentdirectory/events", "SUBSCRIBE", Summary = "Processes an event subscription request")]
    [Route("/Dlna/{UuId}/contentdirectory/events", "UNSUBSCRIBE", Summary = "Processes an event subscription request")]
    public class ProcessContentDirectoryEventRequest
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "SUBSCRIBE,UNSUBSCRIBE")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/connectionmanager/events", "SUBSCRIBE", Summary = "Processes an event subscription request")]
    [Route("/Dlna/{UuId}/connectionmanager/events", "UNSUBSCRIBE", Summary = "Processes an event subscription request")]
    public class ProcessConnectionManagerEventRequest
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "SUBSCRIBE,UNSUBSCRIBE")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/icons/{Filename}", "GET", Summary = "Gets a server icon")]
    [Route("/Dlna/icons/{Filename}", "GET", Summary = "Gets a server icon")]
    public class GetIcon
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UuId { get; set; }

        [ApiMember(Name = "Filename", Description = "The icon filename", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Filename { get; set; }
    }

    public class DlnaServerService : IService, IRequiresRequest
    {
        private readonly IDlnaManager _dlnaManager;

        private const string XMLContentType = "text/xml; charset=UTF-8";

        public IRequest Request { get; set; }
        private IHttpResultFactory _resultFactory;

        private IContentDirectory ContentDirectory
        {
            get
            {
                return DlnaEntryPoint.Current.ContentDirectory;
            }
        }

        private IConnectionManager ConnectionManager
        {
            get
            {
                return DlnaEntryPoint.Current.ConnectionManager;
            }
        }

        private IMediaReceiverRegistrar MediaReceiverRegistrar
        {
            get
            {
                return DlnaEntryPoint.Current.MediaReceiverRegistrar;
            }
        }

        public DlnaServerService(IDlnaManager dlnaManager, IHttpResultFactory httpResultFactory)
        {
            _dlnaManager = dlnaManager;
            _resultFactory = httpResultFactory;
        }

        private string GetHeader(string name)
        {
            return Request.Headers[name];
        }

        public object Get(GetDescriptionXml request)
        {
            var url = Request.AbsoluteUri;
            var serverAddress = url.Substring(0, url.IndexOf("/dlna/", StringComparison.OrdinalIgnoreCase));
            var xml = _dlnaManager.GetServerDescriptionXml(Request.Headers.ToDictionary(), request.UuId, serverAddress);

            var cacheLength = TimeSpan.FromDays(1);
            var cacheKey = Request.RawUrl.GetMD5();
            var bytes = Encoding.UTF8.GetBytes(xml);

            return _resultFactory.GetStaticResult(Request, cacheKey, null, cacheLength, XMLContentType, () => Task.FromResult<Stream>(new MemoryStream(bytes)));
        }

        public object Get(GetContentDirectory request)
        {
            var xml = ContentDirectory.GetServiceXml(Request.Headers.ToDictionary());

            return _resultFactory.GetResult(Request, xml, XMLContentType);
        }

        public object Get(GetMediaReceiverRegistrar request)
        {
            var xml = MediaReceiverRegistrar.GetServiceXml(Request.Headers.ToDictionary());

            return _resultFactory.GetResult(Request, xml, XMLContentType);
        }

        public object Get(GetConnnectionManager request)
        {
            var xml = ConnectionManager.GetServiceXml(Request.Headers.ToDictionary());

            return _resultFactory.GetResult(Request, xml, XMLContentType);
        }

        public object Post(ProcessMediaReceiverRegistrarControlRequest request)
        {
            var response = PostAsync(request.RequestStream, MediaReceiverRegistrar);

            return _resultFactory.GetResult(Request, response.Xml, XMLContentType);
        }

        public object Post(ProcessContentDirectoryControlRequest request)
        {
            var response = PostAsync(request.RequestStream, ContentDirectory);

            return _resultFactory.GetResult(Request, response.Xml, XMLContentType);
        }

        public object Post(ProcessConnectionManagerControlRequest request)
        {
            var response = PostAsync(request.RequestStream, ConnectionManager);

            return _resultFactory.GetResult(Request, response.Xml, XMLContentType);
        }

        private ControlResponse PostAsync(Stream requestStream, IUpnpService service)
        {
            var id = GetPathValue(2);

            return service.ProcessControlRequest(new ControlRequest
            {
                Headers = Request.Headers.ToDictionary(),
                InputXml = requestStream,
                TargetServerUuId = id,
                RequestedUrl = Request.AbsoluteUri
            });
        }

        protected string GetPathValue(int index)
        {
            var pathInfo = Parse(Request.PathInfo);
            var first = pathInfo[0];

            // backwards compatibility
            // TODO: Work out what this is doing.
            if (string.Equals(first, "mediabrowser", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(first, "emby", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(first, "jellyfin", StringComparison.OrdinalIgnoreCase ))
            {
                index++;
            }

            return pathInfo[index];
        }

        private List<string> Parse(string pathUri)
        {
            var actionParts = pathUri.Split(new[] { "://" }, StringSplitOptions.None);

            var pathInfo = actionParts[actionParts.Length - 1];

            var optionsPos = pathInfo.LastIndexOf('?');
            if (optionsPos != -1)
            {
                pathInfo = pathInfo.Substring(0, optionsPos);
            }

            var args = pathInfo.Split('/');

            return args.Skip(1).ToList();
        }

        public object Get(GetIcon request)
        {
            var contentType = "image/" + Path.GetExtension(request.Filename).TrimStart('.').ToLower();

            var cacheLength = TimeSpan.FromDays(365);
            var cacheKey = Request.RawUrl.GetMD5();

            return _resultFactory.GetStaticResult(Request, cacheKey, null, cacheLength, contentType, () => Task.FromResult<Stream>(_dlnaManager.GetIcon(request.Filename).Stream));
        }

        public object Subscribe(ProcessContentDirectoryEventRequest request)
        {
            return ProcessEventRequest(ContentDirectory);
        }

        public object Subscribe(ProcessConnectionManagerEventRequest request)
        {
            return ProcessEventRequest(ConnectionManager);
        }

        public object Subscribe(ProcessMediaReceiverRegistrarEventRequest request)
        {
            return ProcessEventRequest(MediaReceiverRegistrar);
        }

        public object Unsubscribe(ProcessContentDirectoryEventRequest request)
        {
            return ProcessEventRequest(ContentDirectory);
        }

        public object Unsubscribe(ProcessConnectionManagerEventRequest request)
        {
            return ProcessEventRequest(ConnectionManager);
        }

        public object Unsubscribe(ProcessMediaReceiverRegistrarEventRequest request)
        {
            return ProcessEventRequest(MediaReceiverRegistrar);
        }

        private object ProcessEventRequest(IEventManager eventManager)
        {
            var subscriptionId = GetHeader("SID");

            if (string.Equals(Request.Verb, "SUBSCRIBE", StringComparison.OrdinalIgnoreCase))
            {
                var notificationType = GetHeader("NT");

                var callback = GetHeader("CALLBACK");
                var timeoutString = GetHeader("TIMEOUT");

                if (string.IsNullOrEmpty(notificationType))
                {
                    return GetSubscriptionResponse(eventManager.RenewEventSubscription(subscriptionId, notificationType, timeoutString, callback));
                }

                return GetSubscriptionResponse(eventManager.CreateEventSubscription(notificationType, timeoutString, callback));
            }

            return GetSubscriptionResponse(eventManager.CancelEventSubscription(subscriptionId));
        }

        private object GetSubscriptionResponse(EventSubscriptionResponse response)
        {
            return _resultFactory.GetResult(Request, response.Content, response.ContentType, response.Headers);
        }
    }
}
