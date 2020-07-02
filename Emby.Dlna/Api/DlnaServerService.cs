#pragma warning disable CS1591

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Emby.Dlna.Main;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

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
        private const string XMLContentType = "text/xml; charset=UTF-8";

        private readonly IDlnaManager _dlnaManager;
        private readonly IHttpResultFactory _resultFactory;
        private readonly IServerConfigurationManager _configurationManager;

        public IRequest Request { get; set; }

        private IContentDirectory ContentDirectory => DlnaEntryPoint.Current.ContentDirectory;

        private IConnectionManager ConnectionManager => DlnaEntryPoint.Current.ConnectionManager;

        private IMediaReceiverRegistrar MediaReceiverRegistrar => DlnaEntryPoint.Current.MediaReceiverRegistrar;

        public DlnaServerService(
            IDlnaManager dlnaManager,
            IHttpResultFactory httpResultFactory,
            IServerConfigurationManager configurationManager)
        {
            _dlnaManager = dlnaManager;
            _resultFactory = httpResultFactory;
            _configurationManager = configurationManager;
        }

        private string GetHeader(string name)
        {
            return Request.Headers[name];
        }

        public object Get(GetDescriptionXml request)
        {
            var url = Request.AbsoluteUri;
            var serverAddress = url.Substring(0, url.IndexOf("/dlna/", StringComparison.OrdinalIgnoreCase));
            var xml = _dlnaManager.GetServerDescriptionXml(Request.Headers, request.UuId, serverAddress);

            var cacheLength = TimeSpan.FromDays(1);
            var cacheKey = Request.RawUrl.GetMD5();
            var bytes = Encoding.UTF8.GetBytes(xml);

            return _resultFactory.GetStaticResult(Request, cacheKey, null, cacheLength, XMLContentType, () => Task.FromResult<Stream>(new MemoryStream(bytes)));
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetContentDirectory request)
        {
            var xml = ContentDirectory.GetServiceXml();

            return _resultFactory.GetResult(Request, xml, XMLContentType);
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetMediaReceiverRegistrar request)
        {
            var xml = MediaReceiverRegistrar.GetServiceXml();

            return _resultFactory.GetResult(Request, xml, XMLContentType);
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetConnnectionManager request)
        {
            var xml = ConnectionManager.GetServiceXml();

            return _resultFactory.GetResult(Request, xml, XMLContentType);
        }

        public async Task<object> Post(ProcessMediaReceiverRegistrarControlRequest request)
        {
            var response = await PostAsync(request.RequestStream, MediaReceiverRegistrar).ConfigureAwait(false);

            return _resultFactory.GetResult(Request, response.Xml, XMLContentType);
        }

        public async Task<object> Post(ProcessContentDirectoryControlRequest request)
        {
            var response = await PostAsync(request.RequestStream, ContentDirectory).ConfigureAwait(false);

            return _resultFactory.GetResult(Request, response.Xml, XMLContentType);
        }

        public async Task<object> Post(ProcessConnectionManagerControlRequest request)
        {
            var response = await PostAsync(request.RequestStream, ConnectionManager).ConfigureAwait(false);

            return _resultFactory.GetResult(Request, response.Xml, XMLContentType);
        }

        private Task<ControlResponse> PostAsync(Stream requestStream, IUpnpService service)
        {
            var id = GetPathValue(2).ToString();

            return service.ProcessControlRequestAsync(new ControlRequest
            {
                Headers = Request.Headers,
                InputXml = requestStream,
                TargetServerUuId = id,
                RequestedUrl = Request.AbsoluteUri
            });
        }

        // Copied from MediaBrowser.Api/BaseApiService.cs
        // TODO: Remove code duplication
        /// <summary>
        /// Gets the path segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the path segment.</param>
        /// <returns>The path segment at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException" >Path doesn't contain enough segments.</exception>
        /// <exception cref="InvalidDataException" >Path doesn't start with the base url.</exception>
        protected internal ReadOnlySpan<char> GetPathValue(int index)
        {
            static void ThrowIndexOutOfRangeException()
                => throw new IndexOutOfRangeException("Path doesn't contain enough segments.");

            static void ThrowInvalidDataException()
                => throw new InvalidDataException("Path doesn't start with the base url.");

            ReadOnlySpan<char> path = Request.PathInfo;

            // Remove the protocol part from the url
            int pos = path.LastIndexOf("://");
            if (pos != -1)
            {
                path = path.Slice(pos + 3);
            }

            // Remove the query string
            pos = path.LastIndexOf('?');
            if (pos != -1)
            {
                path = path.Slice(0, pos);
            }

            // Remove the domain
            pos = path.IndexOf('/');
            if (pos != -1)
            {
                path = path.Slice(pos);
            }

            // Remove base url
            string baseUrl = _configurationManager.Configuration.BaseUrl;
            int baseUrlLen = baseUrl.Length;
            if (baseUrlLen != 0)
            {
                if (path.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Slice(baseUrlLen);
                }
                else
                {
                    // The path doesn't start with the base url,
                    // how did we get here?
                    ThrowInvalidDataException();
                }
            }

            // Remove leading /
            path = path.Slice(1);

            // Backwards compatibility
            const string Emby = "emby/";
            if (path.StartsWith(Emby, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Slice(Emby.Length);
            }

            const string MediaBrowser = "mediabrowser/";
            if (path.StartsWith(MediaBrowser, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Slice(MediaBrowser.Length);
            }

            // Skip segments until we are at the right index
            for (int i = 0; i < index; i++)
            {
                pos = path.IndexOf('/');
                if (pos == -1)
                {
                    ThrowIndexOutOfRangeException();
                }

                path = path.Slice(pos + 1);
            }

            // Remove the rest
            pos = path.IndexOf('/');
            if (pos != -1)
            {
                path = path.Slice(0, pos);
            }

            return path;
        }

        public object Get(GetIcon request)
        {
            var contentType = "image/" + Path.GetExtension(request.Filename)
                                            .TrimStart('.')
                                            .ToLowerInvariant();

            var cacheLength = TimeSpan.FromDays(365);
            var cacheKey = Request.RawUrl.GetMD5();

            return _resultFactory.GetStaticResult(Request, cacheKey, null, cacheLength, contentType, () => Task.FromResult(_dlnaManager.GetIcon(request.Filename).Stream));
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Subscribe(ProcessContentDirectoryEventRequest request)
        {
            return ProcessEventRequest(ContentDirectory);
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Subscribe(ProcessConnectionManagerEventRequest request)
        {
            return ProcessEventRequest(ConnectionManager);
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Subscribe(ProcessMediaReceiverRegistrarEventRequest request)
        {
            return ProcessEventRequest(MediaReceiverRegistrar);
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Unsubscribe(ProcessContentDirectoryEventRequest request)
        {
            return ProcessEventRequest(ContentDirectory);
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Unsubscribe(ProcessConnectionManagerEventRequest request)
        {
            return ProcessEventRequest(ConnectionManager);
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
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
