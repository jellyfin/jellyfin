using System;
using System.Collections.Generic;
 using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

 namespace Jellyfin.Server.SocketSharp
{
    public class WebSocketSharpListener : IHttpListener
    {
        private HttpListener _listener;

        private readonly ILogger _logger;

        private CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _disposeCancellationToken;

        public WebSocketSharpListener(
            ILogger logger)
        {
            _logger = logger;

            _disposeCancellationToken = _disposeCancellationTokenSource.Token;
        }

        public Func<Exception, IRequest, bool, bool, Task> ErrorHandler { get; set; }
        public Func<IHttpRequest, string, string, string, CancellationToken, Task> RequestHandler { get; set; }

        public Action<WebSocketConnectingEventArgs> WebSocketConnecting { get; set; }

        public Action<WebSocketConnectEventArgs> WebSocketConnected { get; set; }

        public void Start(IEnumerable<string> urlPrefixes)
        {
            // TODO
            //if (_listener == null)
            //{
            //    _listener = new HttpListener(_logger, _cryptoProvider, _socketFactory, _streamHelper, _fileSystem, _environment);
            //}

            //_listener.EnableDualMode = _enableDualMode;

            //if (_certificate != null)
            //{
            //    _listener.LoadCert(_certificate);
            //}

            //_logger.LogInformation("Adding HttpListener prefixes {Prefixes}", urlPrefixes);
            //_listener.Prefixes.AddRange(urlPrefixes);

            //_listener.OnContext = async c => await InitTask(c, _disposeCancellationToken).ConfigureAwait(false);

            //_listener.Start();

            if (_listener == null)
            {
                _listener = new HttpListener();
            }
            
            _logger.LogInformation("Adding HttpListener prefixes {Prefixes}", urlPrefixes);

            //foreach (var urlPrefix in urlPrefixes)
            //{
            //    _listener.Prefixes.Add(urlPrefix);
            //}
            _listener.Prefixes.Add("http://localhost:8096/");

            _listener.Start();

            // TODO how to do this in netcore?
            _listener.BeginGetContext(async c => await InitTask(c, _disposeCancellationToken).ConfigureAwait(false),
                null);
        }

        private static void LogRequest(ILogger logger, HttpListenerRequest request)
        {
            var url = request.Url.ToString();

            logger.LogInformation(
                "{0} {1}. UserAgent: {2}",
                request.IsWebSocketRequest ? "WS" : "HTTP " + request.HttpMethod,
                url,
                request.UserAgent ?? string.Empty);
        }

        private Task InitTask(IAsyncResult asyncResult, CancellationToken cancellationToken)
        {
            var context = _listener.EndGetContext(asyncResult);
            _listener.BeginGetContext(async c => await InitTask(c, _disposeCancellationToken).ConfigureAwait(false), null);
            IHttpRequest httpReq = null;
            var request = context.Request;

            try
            {
                if (request.IsWebSocketRequest)
                {
                    LogRequest(_logger, request);

                    return ProcessWebSocketRequest(context);
                }

                httpReq = GetRequest(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");

                httpReq = httpReq ?? GetRequest(context);
                return ErrorHandler(ex, httpReq, true, true);
            }

            var uri = request.Url;

            return RequestHandler(httpReq, uri.OriginalString, uri.Host, uri.LocalPath, cancellationToken);
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext ctx)
        {
            try
            {
                var endpoint = ctx.Request.RemoteEndPoint.ToString();
                var url = ctx.Request.RawUrl;

                var queryString = new QueryParamCollection(ctx.Request.QueryString);

                var connectingArgs = new WebSocketConnectingEventArgs
                {
                    Url = url,
                    QueryString = queryString,
                    Endpoint = endpoint
                };

                WebSocketConnecting?.Invoke(connectingArgs);

                if (connectingArgs.AllowConnection)
                {
                    _logger.LogDebug("Web socket connection allowed");

                    var webSocketContext = await ctx.AcceptWebSocketAsync(null).ConfigureAwait(false);

                    if (WebSocketConnected != null)
                    {
                        SharpWebSocket socket = null; //new SharpWebSocket(webSocketContext.WebSocket, _logger);
                        await socket.ConnectAsServerAsync().ConfigureAwait(false);

                        WebSocketConnected(new WebSocketConnectEventArgs
                        {
                            Url = url,
                            QueryString = queryString,
                            WebSocket = socket,
                            Endpoint = endpoint
                        });

                        await socket.StartReceive().ConfigureAwait(false);
                    }
                }
                else
                {
                    _logger.LogWarning("Web socket connection not allowed");
                    TryClose(ctx, 401);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AcceptWebSocketAsync error");
                TryClose(ctx, 500);
            }
        }

        private void TryClose(HttpListenerContext ctx, int statusCode)
        {
            try
            {
                ctx.Response.StatusCode = statusCode;
                ctx.Response.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing web socket response");
            }
        }

        private IHttpRequest GetRequest(HttpListenerContext httpContext)
        {
            var urlSegments = httpContext.Request.Url.Segments;

            var operationName = urlSegments[urlSegments.Length - 1];

            var req = new WebSocketSharpRequest(httpContext, operationName, _logger);

            return req;
        }

        public Task Stop()
        {
            _disposeCancellationTokenSource.Cancel();
            _listener?.Close();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used.
        /// </summary>
        /// <param name="disposing">Whether or not the managed resources should be disposed</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Stop().GetAwaiter().GetResult();
            }

            _disposed = true;
        }
    }
}
