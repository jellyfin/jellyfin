using Funq;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.HttpServer.NetListener;
using MediaBrowser.Server.Implementations.HttpServer.SocketSharp;
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public class HttpListenerHost : ServiceStackHost, IHttpServer
    {
        private string HandlerPath { get; set; }
        private string DefaultRedirectPath { get; set; }

        private readonly ILogger _logger;
        public IEnumerable<string> UrlPrefixes { get; private set; }

        private readonly List<IRestfulService> _restServices = new List<IRestfulService>();

        private IHttpListener _listener;

        private readonly ContainerAdapter _containerAdapter;

        public event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        private readonly ConcurrentDictionary<string, string> _localEndPoints = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Gets the local end points.
        /// </summary>
        /// <value>The local end points.</value>
        public IEnumerable<string> LocalEndPoints
        {
            get { return _listener == null ? new List<string>() : _localEndPoints.Keys.ToList(); }
        }

        public HttpListenerHost(IApplicationHost applicationHost, ILogManager logManager, string serviceName, string handlerPath, string defaultRedirectPath, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            DefaultRedirectPath = defaultRedirectPath;
            HandlerPath = handlerPath;

            _logger = logManager.GetLogger("HttpServer");

            _containerAdapter = new ContainerAdapter(applicationHost);
        }

        public override void Configure(Container container)
        {
            HostConfig.Instance.DefaultRedirectPath = DefaultRedirectPath;

            HostConfig.Instance.MapExceptionToStatusCode = new Dictionary<Type, int>
            {
                {typeof (InvalidOperationException), 422},
                {typeof (ResourceNotFoundException), 404},
                {typeof (FileNotFoundException), 404},
                {typeof (DirectoryNotFoundException), 404},
                {typeof (Implementations.Security.AuthenticationException), 401}
            };

            HostConfig.Instance.DebugMode = true;

            HostConfig.Instance.LogFactory = LogManager.LogFactory;

            // The Markdown feature causes slow startup times (5 mins+) on cold boots for some users
            // Custom format allows images
            HostConfig.Instance.EnableFeatures = Feature.Csv | Feature.Html | Feature.Json | Feature.Jsv | Feature.Metadata | Feature.Xml | Feature.CustomFormat;

            container.Adapter = _containerAdapter;

            Plugins.Add(new SwaggerFeature());
            Plugins.Add(new CorsFeature(allowedHeaders: "Content-Type, Authorization, Range, X-MediaBrowser-Token"));

            //Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] {
            //    new SessionAuthProvider(_containerAdapter.Resolve<ISessionContext>()),
            //}));

            PreRequestFilters.Add((httpReq, httpRes) =>
            {
                //Handles Request and closes Responses after emitting global HTTP Headers
                if (string.Equals(httpReq.Verb, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    httpRes.EndRequest(); //add a 'using ServiceStack;'
                }
            });

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

        private void OnRequestReceived(string localEndPoint)
        {
            _localEndPoints.GetOrAdd(localEndPoint, localEndPoint);
        }

        /// <summary>
        /// Starts the Web Service
        /// </summary>
        private void StartListener()
        {
            HostContext.Config.HandlerFactoryPath = ListenerRequest.GetHandlerPathIfAny(UrlPrefixes.First());

            _listener = NativeWebSocket.IsSupported
                ? _listener = new HttpListenerServer(_logger, OnRequestReceived)
                //? _listener = new WebSocketSharpListener(_logger)
                : _listener = new WebSocketSharpListener(_logger, OnRequestReceived);

            _listener.WebSocketHandler = WebSocketHandler;
            _listener.ErrorHandler = ErrorHandler;
            _listener.RequestHandler = RequestHandler;

            _listener.Start(UrlPrefixes);
        }

        private void WebSocketHandler(WebSocketConnectEventArgs args)
        {
            if (WebSocketConnected != null)
            {
                WebSocketConnected(this, args);
            }
        }

        private void ErrorHandler(Exception ex, IRequest httpReq)
        {
            try
            {
                var httpRes = httpReq.Response;

                if (httpRes.IsClosed)
                {
                    return;
                }

                var errorResponse = new ErrorResponse
                {
                    ResponseStatus = new ResponseStatus
                    {
                        ErrorCode = ex.GetType().GetOperationName(),
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                    }
                };

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
                _logger.ErrorException("Error this.ProcessRequest(context)(Exception while writing error to the response)", errorEx);
            }
        }

        /// <summary>
        /// Shut down the Web Service
        /// </summary>
        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Stop();
            }
        }

        /// <summary>
        /// Overridable method that can be used to implement a custom hnandler
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <param name="url">The URL.</param>
        /// <returns>Task.</returns>
        protected Task RequestHandler(IHttpRequest httpReq, Uri url)
        {
            var date = DateTime.Now;

            var httpRes = httpReq.Response;

            var operationName = httpReq.OperationName;
            var localPath = url.LocalPath;

            if (string.Equals(localPath, "/" + HandlerPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                httpRes.RedirectToUrl(DefaultRedirectPath);
                return Task.FromResult(true);
            }
            if (string.Equals(localPath, "/" + HandlerPath, StringComparison.OrdinalIgnoreCase))
            {
                httpRes.RedirectToUrl(HandlerPath + "/" + DefaultRedirectPath);
                return Task.FromResult(true);
            }
            if (string.Equals(localPath, "/", StringComparison.OrdinalIgnoreCase))
            {
                httpRes.RedirectToUrl(HandlerPath + "/" + DefaultRedirectPath);
                return Task.FromResult(true);
            }
            if (string.IsNullOrEmpty(localPath))
            {
                httpRes.RedirectToUrl("/" + HandlerPath + "/" + DefaultRedirectPath);
                return Task.FromResult(true);
            }

            var handler = HttpHandlerFactory.GetHandler(httpReq);

            var remoteIp = httpReq.RemoteIp;

            var serviceStackHandler = handler as IServiceStackHandler;
            if (serviceStackHandler != null)
            {
                var restHandler = serviceStackHandler as RestHandler;
                if (restHandler != null)
                {
                    httpReq.OperationName = operationName = restHandler.RestPath.RequestType.GetOperationName();
                }

                var task = serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, operationName);

                task.ContinueWith(x => httpRes.Close(), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.AttachedToParent);
                //Matches Exceptions handled in HttpListenerBase.InitTask()

                var urlString = url.ToString();

                task.ContinueWith(x =>
                {
                    var statusCode = httpRes.StatusCode;

                    var duration = DateTime.Now - date;

                    LoggerUtils.LogResponse(_logger, statusCode, urlString, remoteIp, duration);

                }, TaskContinuationOptions.None);
                return task;
            }

            return new NotImplementedException("Cannot execute handler: " + handler + " at PathInfo: " + httpReq.PathInfo)
                .AsTaskException();
        }

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

        //public override RouteAttribute[] GetRouteAttributes(System.Type requestType)
        //{
        //    var routes = base.GetRouteAttributes(requestType);
        //    routes.Each(x => x.Path = "/api" + x.Path);
        //    return routes;
        //}

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
    }
}