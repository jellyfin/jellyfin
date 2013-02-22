using Funq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Logging;
using ServiceStack.Api.Swagger;
using ServiceStack.Common.Web;
using ServiceStack.Logging.NLogger;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class HttpServer
    /// </summary>
    public class HttpServer : HttpListenerBase
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
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        private IKernel Kernel { get; set; }

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
        /// Occurs when [web socket connected].
        /// </summary>
        public event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        /// <summary>
        /// Gets the default redirect path.
        /// </summary>
        /// <value>The default redirect path.</value>
        public string DefaultRedirectPath { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServer" /> class.
        /// </summary>
        /// <param name="urlPrefix">The URL.</param>
        /// <param name="serverName">Name of the product.</param>
        /// <param name="applicationHost">The application host.</param>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="defaultRedirectpath">The default redirectpath.</param>
        /// <exception cref="System.ArgumentNullException">urlPrefix</exception>
        public HttpServer(string urlPrefix, string serverName, IApplicationHost applicationHost, IKernel kernel, ILogger logger, string defaultRedirectpath = null)
            : base()
        {
            if (string.IsNullOrEmpty(urlPrefix))
            {
                throw new ArgumentNullException("urlPrefix");
            }
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (applicationHost == null)
            {
                throw new ArgumentNullException("applicationHost");
            }

            DefaultRedirectPath = defaultRedirectpath;
            _logger = logger;
            ApplicationHost = applicationHost;

            EndpointHostConfig.Instance.ServiceStackHandlerFactoryPath = null;
            EndpointHostConfig.Instance.MetadataRedirectPath = "metadata";

            UrlPrefix = urlPrefix;
            Kernel = kernel;

            EndpointHost.ConfigureHost(this, serverName, CreateServiceManager());

            ContentTypeFilters.Register(ContentType.ProtoBuf, (reqCtx, res, stream) => Kernel.ProtobufSerializer.SerializeToStream(res, stream), (type, stream) => Kernel.ProtobufSerializer.DeserializeFromStream(stream, type));

            Init();
            Start(urlPrefix);
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
        /// Configures the specified container.
        /// </summary>
        /// <param name="container">The container.</param>
        public override void Configure(Container container)
        {
            if (!string.IsNullOrEmpty(DefaultRedirectPath))
            {
                SetConfig(new EndpointHostConfig
                {
                    DefaultRedirectPath = DefaultRedirectPath,

                    // Tell SS to bubble exceptions up to here
                    WriteErrorsToResponse = false,

                    DebugMode = true
                });
            }
            
            container.Register(Kernel);
            container.Register(_logger);
            container.Register(ApplicationHost);

            foreach (var service in Kernel.RestServices)
            {
                service.Configure(this);
            }

            Plugins.Add(new SwaggerFeature());
            Plugins.Add(new CorsFeature());

            Serialization.JsonSerializer.Configure();

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
                    WebSocketConnected(this, new WebSocketConnectEventArgs { WebSocket = new NativeWebSocket(webSocketContext.WebSocket, _logger), Endpoint = ctx.Request.RemoteEndPoint });
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

            if (Kernel.Configuration.EnableHttpLevelLogging)
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

            var serviceStackHandler = handler as IServiceStackHttpHandler;

            if (serviceStackHandler != null)
            {
                var restHandler = serviceStackHandler as RestHandler;
                if (restHandler != null)
                {
                    httpReq.OperationName = operationName = restHandler.RestPath.RequestType.Name;
                }
                serviceStackHandler.ProcessRequest(httpReq, httpRes, operationName);
                LogResponse(context);
                httpRes.Close();
                return;
            }

            throw new NotImplementedException("Cannot execute handler: " + handler + " at PathInfo: " + httpReq.PathInfo);
        }

        /// <summary>
        /// Logs the response.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        private void LogResponse(HttpListenerContext ctx)
        {
            var statusode = ctx.Response.StatusCode;

            var log = new StringBuilder();

            log.AppendLine(string.Format("Url: {0}", ctx.Request.Url));

            log.AppendLine("Headers: " + string.Join(",", ctx.Response.Headers.AllKeys.Select(k => k + "=" + ctx.Response.Headers[k])));

            var msg = "Http Response Sent (" + statusode + ") to " + ctx.Request.RemoteEndPoint;

            if (Kernel.Configuration.EnableHttpLevelLogging)
            {
                _logger.LogMultiline(msg, LogSeverity.Debug, log);
            }
        }

        /// <summary>
        /// Creates the service manager.
        /// </summary>
        /// <param name="assembliesWithServices">The assemblies with services.</param>
        /// <returns>ServiceManager.</returns>
        protected override ServiceManager CreateServiceManager(params Assembly[] assembliesWithServices)
        {
            var types = Kernel.RestServices.Select(r => r.GetType()).ToArray();

            return new ServiceManager(new Container(), new ServiceController(() => types));
        }
    }

    /// <summary>
    /// Class WebSocketConnectEventArgs
    /// </summary>
    public class WebSocketConnectEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        /// <value>The web socket.</value>
        public IWebSocket WebSocket { get; set; }
        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        /// <value>The endpoint.</value>
        public IPEndPoint Endpoint { get; set; }
    }
}