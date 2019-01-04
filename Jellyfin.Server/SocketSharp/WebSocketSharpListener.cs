using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Net;
using Emby.Server.Implementations.HttpServer;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Text;
using Microsoft.Extensions.Logging;
using SocketHttpListener.Net;

namespace Jellyfin.SocketSharp
{
    public class WebSocketSharpListener : IHttpListener
    {
        private HttpListener _listener;

        private readonly ILogger _logger;
        private readonly X509Certificate _certificate;
        private readonly IStreamHelper _streamHelper;
        private readonly ITextEncoding _textEncoding;
        private readonly INetworkManager _networkManager;
        private readonly ISocketFactory _socketFactory;
        private readonly ICryptoProvider _cryptoProvider;
        private readonly IFileSystem _fileSystem;
        private readonly bool _enableDualMode;
        private readonly IEnvironmentInfo _environment;

        private CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _disposeCancellationToken;

        public WebSocketSharpListener(ILogger logger, X509Certificate certificate, IStreamHelper streamHelper, ITextEncoding textEncoding, INetworkManager networkManager, ISocketFactory socketFactory, ICryptoProvider cryptoProvider, bool enableDualMode, IFileSystem fileSystem, IEnvironmentInfo environment)
        {
            _logger = logger;
            _certificate = certificate;
            _streamHelper = streamHelper;
            _textEncoding = textEncoding;
            _networkManager = networkManager;
            _socketFactory = socketFactory;
            _cryptoProvider = cryptoProvider;
            _enableDualMode = enableDualMode;
            _fileSystem = fileSystem;
            _environment = environment;

            _disposeCancellationToken = _disposeCancellationTokenSource.Token;
        }

        public Func<Exception, IRequest, bool, bool, Task> ErrorHandler { get; set; }
        public Func<IHttpRequest, string, string, string, CancellationToken, Task> RequestHandler { get; set; }

        public Action<WebSocketConnectingEventArgs> WebSocketConnecting { get; set; }

        public Action<WebSocketConnectEventArgs> WebSocketConnected { get; set; }

        public void Start(IEnumerable<string> urlPrefixes)
        {
            if (_listener == null)
                _listener = new HttpListener(_logger, _cryptoProvider, _socketFactory, _networkManager, _textEncoding, _streamHelper, _fileSystem, _environment);

            _listener.EnableDualMode = _enableDualMode;

            if (_certificate != null)
            {
                _listener.LoadCert(_certificate);
            }

            foreach (var prefix in urlPrefixes)
            {
                _logger.LogInformation("Adding HttpListener prefix " + prefix);
                _listener.Prefixes.Add(prefix);
            }

            _listener.OnContext = ProcessContext;

            _listener.Start();
        }

        private void ProcessContext(HttpListenerContext context)
        {
            //InitTask(context, _disposeCancellationToken);
            Task.Run(() => InitTask(context, _disposeCancellationToken));
        }

        private void LogRequest(ILogger logger, HttpListenerRequest request)
        {
            var url = request.Url.ToString();

            logger.LogInformation("{0} {1}. UserAgent: {2}", request.IsWebSocketRequest ? "WS" : "HTTP " + request.HttpMethod, url, request.UserAgent ?? string.Empty);
        }

        private Task InitTask(HttpListenerContext context, CancellationToken cancellationToken)
        {
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

                var queryString = ctx.Request.QueryString;

                var connectingArgs = new WebSocketConnectingEventArgs
                {
                    Url = url,
                    QueryString = queryString,
                    Endpoint = endpoint
                };

                if (WebSocketConnecting != null)
                {
                    WebSocketConnecting(connectingArgs);
                }

                if (connectingArgs.AllowConnection)
                {
                    _logger.LogDebug("Web socket connection allowed");

                    var webSocketContext = await ctx.AcceptWebSocketAsync(null).ConfigureAwait(false);

                    if (WebSocketConnected != null)
                    {
                        var socket = new SharpWebSocket(webSocketContext.WebSocket, _logger);

                        WebSocketConnected(new WebSocketConnectEventArgs
                        {
                            Url = url,
                            QueryString = queryString,
                            WebSocket = socket,
                            Endpoint = endpoint
                        });

                        await ReceiveWebSocket(ctx, socket).ConfigureAwait(false);
                    }
                }
                else
                {
                    _logger.LogWarning("Web socket connection not allowed");
                    ctx.Response.StatusCode = 401;
                    ctx.Response.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AcceptWebSocketAsync error");
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
        }

        private async Task ReceiveWebSocket(HttpListenerContext ctx, SharpWebSocket socket)
        {
            try
            {
                await socket.StartReceive().ConfigureAwait(false);
            }
            finally
            {
                TryClose(ctx, 200);
            }
        }

        private void TryClose(HttpListenerContext ctx, int statusCode)
        {
            try
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Close();
            }
            catch (ObjectDisposedException)
            {
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

            if (_listener != null)
            {
                _listener.Close();
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private bool _disposed;
        private readonly object _disposeLock = new object();
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

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
    }
}
