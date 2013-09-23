using Funq;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using ServiceStack.Api.Swagger;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;
using System;
using System.Collections.Concurrent;
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

namespace MediaBrowser.Server.Implementations.HttpServer
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
        /// This subscribes to HttpListener requests and finds the appropriate BaseHandler to process it
        /// </summary>
        /// <value>The HTTP listener.</value>
        private IDisposable HttpListener { get; set; }

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
        /// The _container adapter
        /// </summary>
        private readonly ContainerAdapter _containerAdapter;

        private readonly ConcurrentDictionary<string, string> _localEndPoints = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the local end points.
        /// </summary>
        /// <value>The local end points.</value>
        public IEnumerable<string> LocalEndPoints
        {
            get { return _localEndPoints.Keys.ToList(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServer" /> class.
        /// </summary>
        /// <param name="applicationHost">The application host.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="defaultRedirectpath">The default redirectpath.</param>
        /// <exception cref="System.ArgumentNullException">urlPrefix</exception>
        public HttpServer(IApplicationHost applicationHost, ILogManager logManager, string serverName, string defaultRedirectpath)
            : base()
        {
            if (logManager == null)
            {
                throw new ArgumentNullException("logManager");
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
            _logger = logManager.GetLogger("HttpServer");

            LogManager.LogFactory = new ServerLogFactory(logManager);

            EndpointHostConfig.Instance.ServiceStackHandlerFactoryPath = null;
            EndpointHostConfig.Instance.MetadataRedirectPath = "metadata";

            _containerAdapter = new ContainerAdapter(applicationHost);
        }

        /// <summary>
        /// The us culture
        /// </summary>
        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");

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

                MapExceptionToStatusCode = {
                    { typeof(InvalidOperationException), 422 },
                    { typeof(ResourceNotFoundException), 404 },
                    { typeof(FileNotFoundException), 404 },
                    { typeof(DirectoryNotFoundException), 404 }
                },

                DebugMode = true,

                ServiceName = ServerName,

                LogFactory = LogManager.LogFactory,

                // The Markdown feature causes slow startup times (5 mins+) on cold boots for some users
                // Custom format allows images
                EnableFeatures = Feature.Csv | Feature.Html | Feature.Json | Feature.Jsv | Feature.Metadata | Feature.Xml | Feature.CustomFormat
            });

            container.Adapter = _containerAdapter;

            Plugins.Add(new SwaggerFeature());
            Plugins.Add(new CorsFeature());

            ResponseFilters.Add(FilterResponse);
        }

        /// <summary>
        /// Filters the response.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="res">The res.</param>
        /// <param name="dto">The dto.</param>
        private void FilterResponse(IHttpRequest req, IHttpResponse res, object dto)
        {
            var exception = dto as Exception;

            if (exception != null)
            {
                _logger.ErrorException("Error processing request for {0}", exception, req.RawUrl);

                if (!string.IsNullOrEmpty(exception.Message))
                {
                    var error = exception.Message.Replace(Environment.NewLine, " ");
                    error = RemoveControlCharacters(error);

                    res.AddHeader("X-Application-Error-Code", error);
                }
            }

            if (dto is CompressedResult)
            {
                // Per Google PageSpeed
                // This instructs the proxies to cache two versions of the resource: one compressed, and one uncompressed. 
                // The correct version of the resource is delivered based on the client request header. 
                // This is a good choice for applications that are singly homed and depend on public proxies for user locality.                        
                res.AddHeader("Vary", "Accept-Encoding");
            }

            var hasOptions = dto as IHasOptions;

            if (hasOptions != null)
            {
                // Content length has to be explicitly set on on HttpListenerResponse or it won't be happy
                string contentLength;

                if (hasOptions.Options.TryGetValue("Content-Length", out contentLength) && !string.IsNullOrEmpty(contentLength))
                {
                    var length = long.Parse(contentLength, UsCulture);

                    if (length > 0)
                    {
                        var response = (HttpListenerResponse)res.OriginalResponse;

                        response.ContentLength64 = length;

                        // Disable chunked encoding. Technically this is only needed when using Content-Range, but
                        // anytime we know the content length there's no need for it
                        response.SendChunked = false;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the control characters.
        /// </summary>
        /// <param name="inString">The in string.</param>
        /// <returns>System.String.</returns>
        private static string RemoveControlCharacters(string inString)
        {
            if (inString == null) return null;

            var newString = new StringBuilder();

            foreach (var ch in inString)
            {
                if (!char.IsControl(ch))
                {
                    newString.Append(ch);
                }
            }
            return newString.ToString();
        }

        /// <summary>
        /// Starts the Web Service
        /// </summary>
        /// <param name="urlBase">A Uri that acts as the base that the server is listening on.
        /// Format should be: http://127.0.0.1:8080/ or http://127.0.0.1:8080/somevirtual/
        /// Note: the trailing slash is required! For more info see the
        /// HttpListener.Prefixes property on MSDN.</param>
        /// <exception cref="System.ArgumentNullException">urlBase</exception>
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
                _logger.Info("Creating HttpListner");
                Listener = new HttpListener();
            }

            EndpointHost.Config.ServiceStackHandlerFactoryPath = HttpListenerRequestWrapper.GetHandlerPathIfAny(urlBase);

            UrlPrefix = urlBase;

            _logger.Info("Adding HttpListener Prefixes");
            Listener.Prefixes.Add(urlBase);

            IsStarted = true;
            _logger.Info("Starting HttpListner");
            Listener.Start();

            _logger.Info("Creating HttpListner observable stream");
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

            var localPath = context.Request.Url.LocalPath;

            if (string.Equals(localPath, "/mediabrowser/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect(DefaultRedirectPath);
                context.Response.Close();
                return;
            }
            if (string.Equals(localPath, "/mediabrowser", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("mediabrowser/" + DefaultRedirectPath);
                context.Response.Close();
                return;
            }
            if (string.Equals(localPath, "/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("mediabrowser/" + DefaultRedirectPath);
                context.Response.Close();
                return;
            }
            if (string.IsNullOrEmpty(localPath))
            {
                context.Response.Redirect("/mediabrowser/" + DefaultRedirectPath);
                context.Response.Close();
                return;
            }

            RaiseReceiveWebRequest(context);

            await Task.Factory.StartNew(() =>
            {
                try
                {
                    ProcessRequest(context);

                    var url = context.Request.Url.ToString();
                    var endPoint = context.Request.RemoteEndPoint;

                    LogResponse(context, url, endPoint);

                }
                catch (Exception ex)
                {
                    _logger.ErrorException("ProcessRequest failure", ex);
                }

            }).ConfigureAwait(false);
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
            var endpoint = ctx.Request.LocalEndPoint;

            if (endpoint != null)
            {
                var address = endpoint.ToString();

                _localEndPoints.GetOrAdd(address, address);
            }

            if (EnableHttpRequestLogging)
            {
                var log = new StringBuilder();

                log.AppendLine("Url: " + ctx.Request.Url);
                log.AppendLine("Headers: " + string.Join(",", ctx.Request.Headers.AllKeys.Select(k => k + "=" + ctx.Request.Headers[k])));

                var type = ctx.Request.IsWebSocketRequest ? "Web Socket" : "HTTP " + ctx.Request.HttpMethod;

                _logger.LogMultiline(type + " request received from " + ctx.Request.RemoteEndPoint, LogSeverity.Debug, log);
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

            var serviceStackHandler = handler as IServiceStackHttpHandler;

            if (serviceStackHandler != null)
            {
                var restHandler = serviceStackHandler as RestHandler;
                if (restHandler != null)
                {
                    httpReq.OperationName = operationName = restHandler.RestPath.RequestType.Name;
                }
                serviceStackHandler.ProcessRequest(httpReq, httpRes, operationName);
                return;
            }

            throw new NotImplementedException("Cannot execute handler: " + handler + " at PathInfo: " + httpReq.PathInfo);
        }

        /// <summary>
        /// Logs the response.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <param name="url">The URL.</param>
        /// <param name="endPoint">The end point.</param>
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

                        _supportsNativeWebSocket = false;
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

            _logger.Info("Calling EndpointHost.ConfigureHost");

            EndpointHost.ConfigureHost(this, ServerName, CreateServiceManager());

            _logger.Info("Calling ServiceStack AppHost.Init");
            Init();
        }

        /// <summary>
        /// Releases the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public override void Release(object instance)
        {
            // Leave this empty so SS doesn't try to dispose our objects
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