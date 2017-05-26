using MediaBrowser.Controller.Dlna;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api.Dlna
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

    [Route("/Dlna/{UuId}/mediareceiverregistrar/events", Summary = "Processes an event subscription request")]
    public class ProcessMediaReceiverRegistrarEventRequest
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "SUBSCRIBE,POST")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/contentdirectory/events", Summary = "Processes an event subscription request")]
    public class ProcessContentDirectoryEventRequest
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "SUBSCRIBE,POST")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/connectionmanager/events", Summary = "Processes an event subscription request")]
    public class ProcessConnectionManagerEventRequest
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "SUBSCRIBE,POST")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/icons/{Filename}", "GET", Summary = "Gets a server icon")]
    [Route("/Dlna/icons/{Filename}", "GET", Summary = "Gets a server icon")]
    public class GetIcon
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }

        [ApiMember(Name = "Filename", Description = "The icon filename", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Filename { get; set; }
    }

    public class DlnaServerService : BaseApiService
    {
        private readonly IDlnaManager _dlnaManager;
        private readonly IContentDirectory _contentDirectory;
        private readonly IConnectionManager _connectionManager;
        private readonly IMediaReceiverRegistrar _mediaReceiverRegistrar;

        private const string XMLContentType = "text/xml; charset=UTF-8";
        private readonly IMemoryStreamFactory _memoryStreamProvider;

        public DlnaServerService(IDlnaManager dlnaManager, IContentDirectory contentDirectory, IConnectionManager connectionManager, IMediaReceiverRegistrar mediaReceiverRegistrar, IMemoryStreamFactory memoryStreamProvider)
        {
            _dlnaManager = dlnaManager;
            _contentDirectory = contentDirectory;
            _connectionManager = connectionManager;
            _mediaReceiverRegistrar = mediaReceiverRegistrar;
            _memoryStreamProvider = memoryStreamProvider;
        }

        public object Get(GetDescriptionXml request)
        {
            var url = Request.AbsoluteUri;
            var serverAddress = url.Substring(0, url.IndexOf("/dlna/", StringComparison.OrdinalIgnoreCase));
            var xml = _dlnaManager.GetServerDescriptionXml(Request.Headers.ToDictionary(), request.UuId, serverAddress);

            return ResultFactory.GetResult(xml, XMLContentType);
        }

        public object Get(GetContentDirectory request)
        {
            var xml = _contentDirectory.GetServiceXml(Request.Headers.ToDictionary());

            return ResultFactory.GetResult(xml, XMLContentType);
        }

        public object Get(GetMediaReceiverRegistrar request)
        {
            var xml = _mediaReceiverRegistrar.GetServiceXml(Request.Headers.ToDictionary());

            return ResultFactory.GetResult(xml, XMLContentType);
        }

        public object Get(GetConnnectionManager request)
        {
            var xml = _connectionManager.GetServiceXml(Request.Headers.ToDictionary());

            return ResultFactory.GetResult(xml, XMLContentType);
        }

        public object Post(ProcessMediaReceiverRegistrarControlRequest request)
        {
            var response = PostAsync(request.RequestStream, _mediaReceiverRegistrar);

            return ResultFactory.GetResult(response.Xml, XMLContentType);
        }

        public object Post(ProcessContentDirectoryControlRequest request)
        {
            var response = PostAsync(request.RequestStream, _contentDirectory);

            return ResultFactory.GetResult(response.Xml, XMLContentType);
        }

        public object Post(ProcessConnectionManagerControlRequest request)
        {
            var response = PostAsync(request.RequestStream, _connectionManager);

            return ResultFactory.GetResult(response.Xml, XMLContentType);
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

        public object Get(GetIcon request)
        {
            using (var response = _dlnaManager.GetIcon(request.Filename))
            {
                using (var ms = _memoryStreamProvider.CreateNew())
                {
                    response.Stream.CopyTo(ms);

                    ms.Position = 0;
                    var bytes = ms.ToArray();
                    return ResultFactory.GetResult(bytes, "image/" + response.Format.ToString().ToLower());
                }
            }
        }

        public object Any(ProcessContentDirectoryEventRequest request)
        {
            return ProcessEventRequest(_contentDirectory);
        }

        public object Any(ProcessConnectionManagerEventRequest request)
        {
            return ProcessEventRequest(_connectionManager);
        }

        public object Any(ProcessMediaReceiverRegistrarEventRequest request)
        {
            return ProcessEventRequest(_mediaReceiverRegistrar);
        }

        private object ProcessEventRequest(IEventManager eventManager)
        {
            var subscriptionId = GetHeader("SID");
            var notificationType = GetHeader("NT");
            var callback = GetHeader("CALLBACK");
            var timeoutString = GetHeader("TIMEOUT");

            var timeout = ParseTimeout(timeoutString);

            if (string.Equals(Request.Verb, "SUBSCRIBE", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(notificationType))
                {
                    return GetSubscriptionResponse(eventManager.RenewEventSubscription(subscriptionId, timeout));
                }

                return GetSubscriptionResponse(eventManager.CreateEventSubscription(notificationType, timeout, callback));
            }

            return GetSubscriptionResponse(eventManager.CancelEventSubscription(subscriptionId));
        }

        private object GetSubscriptionResponse(EventSubscriptionResponse response)
        {
            return ResultFactory.GetResult(response.Content, response.ContentType, response.Headers);
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private int? ParseTimeout(string header)
        {
            if (!string.IsNullOrEmpty(header))
            {
                // Starts with SECOND-
                header = header.Split('-').Last();

                int val;

                if (int.TryParse(header, NumberStyles.Any, _usCulture, out val))
                {
                    return val;
                }
            }

            return null;
        }
    }
}
