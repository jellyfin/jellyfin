using Funq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using ServiceStack.Api.Swagger;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.Logging.NLogger;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.HttpServer
{
    /// <summary>
    /// Class HttpServer
    /// </summary>
    public class HttpServer : HttpListenerBase, IHttpServer
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the URL prefix.
        /// </summary>
        /// <value>The URL prefix.</value>
        public string UrlPrefix { get; private set; }

        /// <summary>
        /// The _rest services
        /// </summary>
        private readonly List<IRestfulService> _restServices = new List<IRestfulService>(); 

        /// <summary>
        /// Gets or sets the application host.
        /// </summary>
        /// <value>The application host.</value>
        private IApplicationHost ApplicationHost { get; set; }

        /// <summary>
        /// This subscribes to HttpListener requests and finds the appropriate BaseHandler to process it
        /// </summary>
        /// <value>The HTTP listener.</value>
        private IDisposable HttpListener { get; set; }

        /// <summary>
        /// Gets or sets the protobuf serializer.
        /// </summary>
        /// <value>The protobuf serializer.</value>
        private IProtobufSerializer ProtobufSerializer { get; set; }
        
        /// <summary>
        /// Occurs when [web socket connected].
        /// </summary>
        public event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        /// <summary>
        /// Gets the default redirect path.
        /// </summary>
        /// <value>The default redirect path.</value>
        private string DefaultRedirectPath { get; set; }

        /// <summary>
        /// Gets or sets the name of the server.
        /// </summary>
        /// <value>The name of the server.</value>
        private string ServerName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServer" /> class.
        /// </summary>
        /// <param name="applicationHost">The application host.</param>
        /// <param name="protobufSerializer">The protobuf serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="defaultRedirectpath">The default redirectpath.</param>
        /// <exception cref="System.ArgumentNullException">urlPrefix</exception>
        public HttpServer(IApplicationHost applicationHost, IProtobufSerializer protobufSerializer, ILogger logger, string serverName, string defaultRedirectpath)
            : base()
        {
            if (protobufSerializer == null)
            {
                throw new ArgumentNullException("protobufSerializer");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (applicationHost == null)
            {
                throw new ArgumentNullException("applicationHost");
            }
            if (string.IsNullOrEmpty(serverName))
            {
                throw new ArgumentNullException("serverName");
            }
            if (string.IsNullOrEmpty(defaultRedirectpath))
            {
                throw new ArgumentNullException("defaultRedirectpath");
            }

            ServerName = serverName;
            DefaultRedirectPath = defaultRedirectpath;
            ProtobufSerializer = protobufSerializer;
            _logger = logger;
            ApplicationHost = applicationHost;

            EndpointHostConfig.Instance.ServiceStackHandlerFactoryPath = null;
            EndpointHostConfig.Instance.MetadataRedirectPath = "metadata";
        }

        /// <summary>
        /// Configures the specified container.
        /// </summary>
        /// <param name="container">The container.</param>
        public override void Configure(Container container)
        {
            JsConfig.DateHandler = JsonDateHandler.ISO8601;
            JsConfig.ExcludeTypeInfo = true;
            JsConfig.IncludeNullValues = false;
            
            SetConfig(new EndpointHostConfig
            {
                DefaultRedirectPath = DefaultRedirectPath,

                // Tell SS to bubble exceptions up to here
                WriteErrorsToResponse = false,

                DebugMode = true
            });

            container.Adapter = new ContainerAdapter(ApplicationHost);

            Plugins.Add(new SwaggerFeature());
            Plugins.Add(new CorsFeature());

            ServiceStack.Logging.LogManager.LogFactory = new NLogFactory();
        }

        /// <summary>
        /// Starts the Web Service
        /// </summary>
        /// <param name="urlBase">A Uri that acts as the base that the server is listening on.
        /// Format should be: http://127.0.0.1:8080/ or http://127.0.0.1:8080/somevirtual/
        /// Note: the trailing slash is required! For more info see the
        /// HttpListener.Prefixes property on MSDN.</param>
        public override void Start(string urlBase)
        {
            if (string.IsNullOrEmpty(urlBase))
            {
                throw new ArgumentNullException("urlBase");
            }

            // *** Already running - just leave it in place
            if (IsStarted)
            {
                return;
            }

            if (Listener == null)
            {
                Listener = new HttpListener();
            }

            EndpointHost.Config.ServiceStackHandlerFactoryPath = HttpListenerRequestWrapper.GetHandlerPathIfAny(urlBase);

            UrlPrefix = urlBase;

            Listener.Prefixes.Add(urlBase);

            IsStarted = true;
            Listener.Start();

            HttpListener = CreateObservableStream().Subscribe(ProcessHttpRequestAsync);
        }

        /// <summary>
        /// Creates the observable stream.
        /// </summary>
        /// <returns>IObservable{HttpListenerContext}.</returns>
        private IObservable<HttpListenerContext> CreateObservableStream()
        {
            return Observable.Create<HttpListenerContext>(obs =>
                                Observable.FromAsync(() => Listener.GetContextAsync())
                                          .Subscribe(obs))
                             .Repeat()
                             .Retry()
                             .Publish()
                             .RefCount();
        }

        /// <summary>
        /// Processes incoming http requests by routing them to the appropiate handler
        /// </summary>
        /// <param name="context">The CTX.</param>
        private async void ProcessHttpRequestAsync(HttpListenerContext context)
        {
            LogHttpRequest(context);

            if (context.Request.IsWebSocketRequest)
            {
                await ProcessWebSocketRequest(context).ConfigureAwait(false);
                return;
            }


            Task.Run(() =>
            {
                RaiseReceiveWebRequest(context);

                try
                {
                    ProcessRequest(context);
                }
                catch (InvalidOperationException ex)
                {
                    HandleException(context.Response, ex, 422);

                    throw;
                }
                catch (ResourceNotFoundException ex)
                {
                    HandleException(context.Response, ex, 404);

                    throw;
                }
                catch (FileNotFoundException ex)
                {
                    HandleException(context.Response, ex, 404);

                    throw;
                }
                catch (DirectoryNotFoundException ex)
                {
                    HandleException(context.Response, ex, 404);

                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    HandleException(context.Response, ex, 401);

                    throw;
                }
                catch (ArgumentException ex)
                {
                    HandleException(context.Response, ex, 400);

                    throw;
                }
                catch (Exception ex)
                {
                    HandleException(context.Response, ex, 500);

                    throw;
                }
            });
        }

        /// <summary>
        /// Processes the web socket request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        private async Task ProcessWebSocketRequest(HttpListenerContext ctx)
        {
            try
            {
                var webSocketContext = await ctx.AcceptWebSocketAsync(null).ConfigureAwait(false);

                if (WebSocketConnected != null)
                {
                    WebSocketConnected(this, new WebSocketConnectEventArgs { WebSocket = new NativeWebSocket(webSocketContext.WebSocket, _logger), Endpoint = ctx.Request.RemoteEndPoint.ToString() });
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("AcceptWebSocketAsync error", ex);

                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
        }

        /// <summary>
        /// Logs the HTTP request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        private void LogHttpRequest(HttpListenerContext ctx)
        {
            var log = new StringBuilder();

            log.AppendLine("Url: " + ctx.Request.Url);
            log.AppendLine("Headers: " + string.Join(",", ctx.Request.Headers.AllKeys.Select(k => k + "=" + ctx.Request.Headers[k])));

            var type = ctx.Request.IsWebSocketRequest ? "Web Socket" : "HTTP " + ctx.Request.HttpMethod;

            if (EnableHttpRequestLogging)
            {
                _logger.LogMultiline(type + " request received from " + ctx.Request.RemoteEndPoint, LogSeverity.Debug, log);
            }
        }

        /// <summary>
        /// Appends the error message.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        private void HandleException(HttpListenerResponse response, Exception ex, int statusCode)
        {
            _logger.ErrorException("Error processing request", ex);

            response.StatusCode = statusCode;

            response.Headers.Add("Status", statusCode.ToString(new CultureInfo("en-US")));

            response.Headers.Remove("Age");
            response.Headers.Remove("Expires");
            response.Headers.Remove("Cache-Control");
            response.Headers.Remove("Etag");
            response.Headers.Remove("Last-Modified");

            response.ContentType = "text/plain";

            if (!string.IsNullOrEmpty(ex.Message))
            {
                response.AddHeader("X-Application-Error-Code", ex.Message);
            }

            // This could fail, but try to add the stack trace as the body content
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("\"ResponseStatus\":{");
                sb.AppendFormat(" \"ErrorCode\":{0},\n", ex.GetType().Name.EncodeJson());
                sb.AppendFormat(" \"Message\":{0},\n", ex.Message.EncodeJson());
                sb.AppendFormat(" \"StackTrace\":{0}\n", ex.StackTrace.EncodeJson());
                sb.AppendLine("}");
                sb.AppendLine("}");

                response.StatusCode = 500;
                response.ContentType = ContentType.Json;
                var sbBytes = sb.ToString().ToUtf8Bytes();
                response.OutputStream.Write(sbBytes, 0, sbBytes.Length);
                response.Close();
            }
            catch (Exception errorEx)
            {
                _logger.ErrorException("Error processing failed request", errorEx);
            }
        }


        /// <summary>
        /// Overridable method that can be used to implement a custom hnandler
        /// </summary>
        /// <param name="context">The context.</param>
        /// <exception cref="System.NotImplementedException">Cannot execute handler:  + handler +  at PathInfo:  + httpReq.PathInfo</exception>
        protected override void ProcessRequest(HttpListenerContext context)
        {
            if (string.IsNullOrEmpty(context.Request.RawUrl)) return;

            var operationName = context.Request.GetOperationName();

            var httpReq = new HttpListenerRequestWrapper(operationName, context.Request);
            var httpRes = new HttpListenerResponseWrapper(context.Response);
            var handler = ServiceStackHttpHandlerFactory.GetHandler(httpReq);

            var url = context.Request.Url.ToString();
            var endPoint = context.Request.RemoteEndPoint;

            var serviceStackHandler = handler as IServiceStackHttpHandler;

            if (serviceStackHandler != null)
            {
                var restHandler = serviceStackHandler as RestHandler;
                if (restHandler != null)
                {
                    httpReq.OperationName = operationName = restHandler.RestPath.RequestType.Name;
                }
                serviceStackHandler.ProcessRequest(httpReq, httpRes, operationName);
                LogResponse(context, url, endPoint);
                httpRes.Close();
                return;
            }

            throw new NotImplementedException("Cannot execute handler: " + handler + " at PathInfo: " + httpReq.PathInfo);
        }

        /// <summary>
        /// Logs the response.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        private void LogResponse(HttpListenerContext ctx, string url, IPEndPoint endPoint)
        {
            if (!EnableHttpRequestLogging)
            {
                return;
            }
            
            var statusode = ctx.Response.StatusCode;

            var log = new StringBuilder();

            log.AppendLine(string.Format("Url: {0}", url));

            log.AppendLine("Headers: " + string.Join(",", ctx.Response.Headers.AllKeys.Select(k => k + "=" + ctx.Response.Headers[k])));

            var msg = "Http Response Sent (" + statusode + ") to " + endPoint;

            _logger.LogMultiline(msg, LogSeverity.Debug, log);
        }

        /// <summary>
        /// Creates the service manager.
        /// </summary>
        /// <param name="assembliesWithServices">The assemblies with services.</param>
        /// <returns>ServiceManager.</returns>
        protected override ServiceManager CreateServiceManager(params Assembly[] assembliesWithServices)
        {
            var types = _restServices.Select(r => r.GetType()).ToArray();

            return new ServiceManager(new Container(), new ServiceController(() => types));
        }

        /// <summary>
        /// Shut down the Web Service
        /// </summary>
        public override void Stop()
        {
            if (HttpListener != null)
            {
                HttpListener.Dispose();
                HttpListener = null;
            }

            if (Listener != null)
            {
                Listener.Prefixes.Remove(UrlPrefix);
            }

            base.Stop();
        }

        /// <summary>
        /// The _supports native web socket
        /// </summary>
        private bool? _supportsNativeWebSocket;

        /// <summary>
        /// Gets a value indicating whether [supports web sockets].
        /// </summary>
        /// <value><c>true</c> if [supports web sockets]; otherwise, <c>false</c>.</value>
        public bool SupportsWebSockets
        {
            get
            {
                if (!_supportsNativeWebSocket.HasValue)
                {
                    try
                    {
                        new ClientWebSocket();

                        _supportsNativeWebSocket = true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                        _supportsNativeWebSocket = false;
                    }
                }

                return _supportsNativeWebSocket.Value;
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether [enable HTTP request logging].
        /// </summary>
        /// <value><c>true</c> if [enable HTTP request logging]; otherwise, <c>false</c>.</value>
        public bool EnableHttpRequestLogging { get; set; }

        /// <summary>
        /// Adds the rest handlers.
        /// </summary>
        /// <param name="services">The services.</param>
        public void Init(IEnumerable<IRestfulService> services)
        {
            _restServices.AddRange(services);

            EndpointHost.ConfigureHost(this, ServerName, CreateServiceManager());
            ContentTypeFilters.Register(ContentType.ProtoBuf, (reqCtx, res, stream) => ProtobufSerializer.SerializeToStream(res, stream), (type, stream) => ProtobufSerializer.DeserializeFromStream(stream, type));
            
            foreach (var route in services.SelectMany(i => i.GetRoutes()))
            {
                Routes.Add(route.RequestType, route.Path, route.Verbs);
            }

            Init();
        }
    }

    /// <summary>
    /// Class ContainerAdapter
    /// </summary>
    class ContainerAdapter : IContainerAdapter, IRelease
    {
        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerAdapter" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        public ContainerAdapter(IApplicationHost appHost)
        {
            _appHost = appHost;
        }
        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T Resolve<T>()
        {
            return _appHost.Resolve<T>();
        }

        /// <summary>
        /// Tries the resolve.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T TryResolve<T>()
        {
            return _appHost.TryResolve<T>();
        }

        /// <summary>
        /// Releases the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public void Release(object instance)
        {
            // Leave this empty so SS doesn't try to dispose our objects
        }
    }
}