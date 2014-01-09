using Funq;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using ServiceStack;
using ServiceStack.Api.Swagger;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Host.HttpListener;
using ServiceStack.Logging;
using ServiceStack.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public delegate void DelReceiveWebRequest(HttpListenerContext context);

    public class HttpListenerHost : ServiceStackHost, IHttpServer
    {
        private string ServerName { get; set; }
        private string HandlerPath { get; set; }
        private string DefaultRedirectPath { get; set; }

        private readonly ILogger _logger;
        public IEnumerable<string> UrlPrefixes { get; private set; }

        private readonly List<IRestfulService> _restServices = new List<IRestfulService>();

        private HttpListener Listener { get; set; }
        protected bool IsStarted = false;

        private readonly List<AutoResetEvent> _autoResetEvents = new List<AutoResetEvent>();

        private readonly ContainerAdapter _containerAdapter;

        private readonly ConcurrentDictionary<string, string> _localEndPoints = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        /// <summary>
        /// Gets the local end points.
        /// </summary>
        /// <value>The local end points.</value>
        public IEnumerable<string> LocalEndPoints
        {
            get { return _localEndPoints.Keys.ToList(); }
        }

        public HttpListenerHost(IApplicationHost applicationHost, ILogManager logManager, string serviceName, string handlerPath, string defaultRedirectPath, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            DefaultRedirectPath = defaultRedirectPath;
            ServerName = serviceName;
            HandlerPath = handlerPath;

            _logger = logManager.GetLogger("HttpServer");

            _containerAdapter = new ContainerAdapter(applicationHost);

            for (var i = 0; i < 1; i++)
            {
                _autoResetEvents.Add(new AutoResetEvent(false));
            }
        }

        public override void Configure(Container container)
        {
            HostConfig.Instance.DefaultRedirectPath = DefaultRedirectPath;

            HostConfig.Instance.MapExceptionToStatusCode = new Dictionary<Type, int>
            {
                {typeof (InvalidOperationException), 422},
                {typeof (ResourceNotFoundException), 404},
                {typeof (FileNotFoundException), 404},
                {typeof (DirectoryNotFoundException), 404}
            };

            HostConfig.Instance.DebugMode = true;

            HostConfig.Instance.LogFactory = LogManager.LogFactory;

            // The Markdown feature causes slow startup times (5 mins+) on cold boots for some users
            // Custom format allows images
            HostConfig.Instance.EnableFeatures = Feature.Csv | Feature.Html | Feature.Json | Feature.Jsv | Feature.Metadata | Feature.Xml | Feature.CustomFormat;

            container.Adapter = _containerAdapter;

            Plugins.Add(new SwaggerFeature());
            Plugins.Add(new CorsFeature());
            HostContext.GlobalResponseFilters.Add(new ResponseFilter(_logger).FilterResponse);
        }

        public override void OnAfterInit()
        {
            SetAppDomainData();

            base.OnAfterInit();
        }

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();

            Config.HandlerFactoryPath = string.IsNullOrEmpty(HandlerPath)
                ? null
                : HandlerPath;

            Config.MetadataRedirectPath = string.IsNullOrEmpty(HandlerPath)
                ? "metadata"
                : PathUtils.CombinePaths(HandlerPath, "metadata");
        }

        protected override ServiceController CreateServiceController(params Assembly[] assembliesWithServices)
        {
            var types = _restServices.Select(r => r.GetType()).ToArray();

            return new ServiceController(this, () => types);
        }

        public virtual void SetAppDomainData()
        {
            //Required for Mono to resolve VirtualPathUtility and Url.Content urls
            var domain = Thread.GetDomain(); // or AppDomain.Current
            domain.SetData(".appDomain", "1");
            domain.SetData(".appVPath", "/");
            domain.SetData(".appPath", domain.BaseDirectory);
            if (string.IsNullOrEmpty(domain.GetData(".appId") as string))
            {
                domain.SetData(".appId", "1");
            }
            if (string.IsNullOrEmpty(domain.GetData(".domainId") as string))
            {
                domain.SetData(".domainId", "1");
            }
        }

        public override ServiceStackHost Start(string listeningAtUrlBase)
        {
            StartListener();
            return this;
        }

        /// <summary>
        /// Starts the Web Service
        /// </summary>
        private void StartListener()
        {
            // *** Already running - just leave it in place
            if (IsStarted)
                return;

            if (Listener == null)
                Listener = new HttpListener();

            HostContext.Config.HandlerFactoryPath = ListenerRequest.GetHandlerPathIfAny(UrlPrefixes.First());

            foreach (var prefix in UrlPrefixes)
            {
                _logger.Info("Adding HttpListener prefix " + prefix);
                Listener.Prefixes.Add(prefix);
            }

            IsStarted = true;
            _logger.Info("Starting HttpListner");
            Listener.Start();

            for (var i = 0; i < _autoResetEvents.Count; i++)
            {
                var index = i;
                ThreadPool.QueueUserWorkItem(o => Listen(o, index));
            }
        }

        private bool IsListening
        {
            get { return this.IsStarted && this.Listener != null && this.Listener.IsListening; }
        }

        // Loop here to begin processing of new requests.
        private void Listen(object state, int index)
        {
            while (IsListening)
            {
                if (Listener == null) return;

                try
                {
                    Listener.BeginGetContext(c => ListenerCallback(c, index), Listener);

                    _autoResetEvents[index].WaitOne();
                }
                catch (Exception ex)
                {
                    _logger.Error("Listen()", ex);
                    return;
                }
                if (Listener == null) return;
            }
        }

        // Handle the processing of a request in here.
        private void ListenerCallback(IAsyncResult asyncResult, int index)
        {
            var listener = asyncResult.AsyncState as HttpListener;
            HttpListenerContext context = null;

            if (listener == null) return;

            try
            {
                if (!IsListening)
                {
                    _logger.Debug("Ignoring ListenerCallback() as HttpListener is no longer listening");
                    return;
                }
                // The EndGetContext() method, as with all Begin/End asynchronous methods in the .NET Framework,
                // blocks until there is a request to be processed or some type of data is available.
                context = listener.EndGetContext(asyncResult);
            }
            catch (Exception ex)
            {
                // You will get an exception when httpListener.Stop() is called
                // because there will be a thread stopped waiting on the .EndGetContext()
                // method, and again, that is just the way most Begin/End asynchronous
                // methods of the .NET Framework work.
                var errMsg = ex + ": " + IsListening;
                _logger.Warn(errMsg);
                return;
            }
            finally
            {
                // Once we know we have a request (or exception), we signal the other thread
                // so that it calls the BeginGetContext() (or possibly exits if we're not
                // listening any more) method to start handling the next incoming request
                // while we continue to process this request on a different thread.
                _autoResetEvents[index].Set();
            }

            if (context == null) return;

            var date = DateTime.Now;

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    LogHttpRequest(context, index);

                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
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

                    var url = context.Request.Url.ToString();
                    var endPoint = context.Request.RemoteEndPoint;

                    await ProcessRequestAsync(context).ConfigureAwait(false);

                    var duration = DateTime.Now - date;

                    if (EnableHttpRequestLogging)
                    {
                        LoggerUtils.LogResponse(_logger, context, url, endPoint, duration);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("ProcessRequest failure", ex);

                    HandleError(ex, context, _logger);
                }

            });
        }

        /// <summary>
        /// Logs the HTTP request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        private void LogHttpRequest(HttpListenerContext ctx, int index)
        {
            var endpoint = ctx.Request.LocalEndPoint;

            if (endpoint != null)
            {
                var address = endpoint.ToString();

                _localEndPoints.GetOrAdd(address, address);
            }

            if (EnableHttpRequestLogging)
            {
                LoggerUtils.LogRequest(_logger, ctx, index);
            }
        }

        /// <summary>
        /// Processes the web socket request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        private async Task ProcessWebSocketRequest(HttpListenerContext ctx)
        {
#if !__MonoCS__
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
#endif
        }

        public static void HandleError(Exception ex, HttpListenerContext context, ILogger logger)
        {
            try
            {
                var errorResponse = new ErrorResponse
                {
                    ResponseStatus = new ResponseStatus
                    {
                        ErrorCode = ex.GetType().GetOperationName(),
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                    }
                };

                var operationName = context.Request.GetOperationName();
                var httpReq = context.ToRequest(operationName);
                var httpRes = httpReq.Response;
                var contentType = httpReq.ResponseContentType;

                var serializer = HostContext.ContentTypes.GetResponseSerializer(contentType);
                if (serializer == null)
                {
                    contentType = HostContext.Config.DefaultContentType;
                    serializer = HostContext.ContentTypes.GetResponseSerializer(contentType);
                }

                var httpError = ex as IHttpError;
                if (httpError != null)
                {
                    httpRes.StatusCode = httpError.Status;
                    httpRes.StatusDescription = httpError.StatusDescription;
                }
                else
                {
                    httpRes.StatusCode = 500;
                }

                httpRes.ContentType = contentType;

                serializer(httpReq, errorResponse, httpRes);

                httpRes.Close();
            }
            catch (Exception errorEx)
            {
                logger.ErrorException("Error this.ProcessRequest(context)(Exception while writing error to the response)", errorEx);
            }
        }

        /// <summary>
        /// Shut down the Web Service
        /// </summary>
        public void Stop()
        {
            if (Listener != null)
            {
                foreach (var prefix in UrlPrefixes)
                {
                    Listener.Prefixes.Remove(prefix);
                }

                Listener.Close();
            }
        }

        /// <summary>
        /// Overridable method that can be used to implement a custom hnandler
        /// </summary>
        /// <param name="context"></param>
        protected Task ProcessRequestAsync(HttpListenerContext context)
        {
            if (string.IsNullOrEmpty(context.Request.RawUrl))
                return ((object)null).AsTaskResult();

            var operationName = context.Request.GetOperationName();

            var httpReq = context.ToRequest(operationName);
            var httpRes = httpReq.Response;
            var handler = HttpHandlerFactory.GetHandler(httpReq);

            var serviceStackHandler = handler as IServiceStackHandler;
            if (serviceStackHandler != null)
            {
                var restHandler = serviceStackHandler as RestHandler;
                if (restHandler != null)
                {
                    httpReq.OperationName = operationName = restHandler.RestPath.RequestType.GetOperationName();
                }

                var task = serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, operationName);
                task.ContinueWith(x => httpRes.Close());

                return task;
            }

            return new NotImplementedException("Cannot execute handler: " + handler + " at PathInfo: " + httpReq.PathInfo)
                .AsTaskException();
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

            ServiceController = CreateServiceController();

            _logger.Info("Calling ServiceStack AppHost.Init");

            base.Init();
        }

        /// <summary>
        /// Releases the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public override void Release(object instance)
        {
            // Leave this empty so SS doesn't try to dispose our objects
        }

        private bool _disposed;
        private readonly object _disposeLock = new object();
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            base.Dispose();

            lock (_disposeLock)
            {
                if (_disposed) return;

                if (disposing)
                {
                    Stop();
                }

                //release unmanaged resources here...
                _disposed = true;
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void StartServer(IEnumerable<string> urlPrefixes)
        {
            UrlPrefixes = urlPrefixes.ToList();
            Start(UrlPrefixes.First());
        }

        public bool SupportsWebSockets
        {
            get { return NativeWebSocket.IsSupported; }
        }
    }
}