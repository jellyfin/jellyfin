using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using ServiceStack;
using ServiceStack.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace MediaBrowser.Server.Implementations.HttpServer.SocketSharp
{
    public class WebSocketSharpListener : IHttpListener
    {
        private readonly ConcurrentDictionary<string, string> _localEndPoints = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private WebSocketSharp.Server.HttpServer _httpsv;

        private readonly ILogger _logger;

        public WebSocketSharpListener(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<string> LocalEndPoints
        {
            get { return _localEndPoints.Keys.ToList(); }
        }

        public Action<Exception, IRequest> ErrorHandler { get; set; }

        public Func<IHttpRequest, Uri, Task> RequestHandler { get; set; }

        public Action<WebSocketConnectEventArgs> WebSocketHandler { get; set; }

        public void Start(IEnumerable<string> urlPrefixes)
        {
            _httpsv = new WebSocketSharp.Server.HttpServer(8096, false, urlPrefixes.First());

            _httpsv.OnRequest += _httpsv_OnRequest;

            _httpsv.Start();
        }

        void _httpsv_OnRequest(object sender, HttpRequestEventArgs e)
        {
            Task.Factory.StartNew(() => InitTask(e.Context));
        }

        private void InitTask(HttpListenerContext context)
        {
            try
            {
                var task = this.ProcessRequestAsync(context);
                task.ContinueWith(x => HandleError(x.Exception, context), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.AttachedToParent);

                if (task.Status == TaskStatus.Created)
                {
                    task.RunSynchronously();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, context);
            }
        }

        private Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;

            LogHttpRequest(request);

            if (request.IsWebSocketRequest)
            {
                ProcessWebSocketRequest(context);
                return Task.FromResult(true);
            }

            if (string.IsNullOrEmpty(context.Request.RawUrl))
                return ((object)null).AsTaskResult();

            var httpReq = GetRequest(context);

            return RequestHandler(httpReq, request.Url);
        }

        /// <summary>
        /// Logs the HTTP request.
        /// </summary>
        /// <param name="request">The request.</param>
        private void LogHttpRequest(HttpListenerRequest request)
        {
            var endpoint = request.LocalEndPoint;

            if (endpoint != null)
            {
                var address = endpoint.ToString();

                _localEndPoints.GetOrAdd(address, address);
            }

            LogRequest(_logger, request);
        }

        private void ProcessWebSocketRequest(HttpListenerContext ctx)
        {
            try
            {
                var webSocketContext = ctx.AcceptWebSocket(null);

                if (WebSocketHandler != null)
                {
                    WebSocketHandler(new WebSocketConnectEventArgs
                    {
                        WebSocket = new SharpWebSocket(webSocketContext.WebSocket, _logger),
                        Endpoint = ctx.Request.RemoteEndPoint.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("AcceptWebSocketAsync error", ex);
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
        }

        private IHttpRequest GetRequest(HttpListenerContext httpContext)
        {
            var operationName = httpContext.Request.GetOperationName();

            var req = new WebSocketSharpRequest(httpContext, operationName, RequestAttributes.None, _logger);
            req.RequestAttributes = req.GetAttributes();

            return req;
        }

        /// <summary>
        /// Logs the request.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="request">The request.</param>
        private static void LogRequest(ILogger logger, HttpListenerRequest request)
        {
            var log = new StringBuilder();

            //var headers = string.Join(",", request.Headers.AllKeys.Where(i => !string.Equals(i, "cookie", StringComparison.OrdinalIgnoreCase) && !string.Equals(i, "Referer", StringComparison.OrdinalIgnoreCase)).Select(k => k + "=" + request.Headers[k]));

            //log.AppendLine("Ip: " + request.RemoteEndPoint + ". Headers: " + headers);

            var type = request.IsWebSocketRequest ? "Web Socket" : "HTTP " + request.HttpMethod;

            logger.LogMultiline(type + " " + request.Url, LogSeverity.Debug, log);
        }

        private void HandleError(Exception ex, HttpListenerContext context)
        {
            var httpReq = GetRequest(context);

            if (ErrorHandler != null)
            {
                ErrorHandler(ex, httpReq);
            }
        }

        public void Stop()
        {
            _httpsv.Stop();
        }

        private readonly object _disposeLock = new object();
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_httpsv != null)
                {
                    _httpsv.OnRequest -= _httpsv_OnRequest;
                    _httpsv.Stop();
                    _httpsv = null;
                }
            }
        }
    }
}
