using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using SocketHttpListener.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;

namespace Emby.Server.Implementations.HttpServer.SocketSharp
{
    public class WebSocketSharpListener : IHttpListener
    {
        private HttpListener _listener;

        private readonly ILogger _logger;
        private readonly X509Certificate _certificate;
        private readonly IMemoryStreamFactory _memoryStreamProvider;
        private readonly ITextEncoding _textEncoding;
        private readonly INetworkManager _networkManager;
        private readonly ISocketFactory _socketFactory;
        private readonly ICryptoProvider _cryptoProvider;
        private readonly IFileSystem _fileSystem;
        private readonly bool _enableDualMode;
        private readonly IEnvironmentInfo _environment;

        private CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _disposeCancellationToken;

        public WebSocketSharpListener(ILogger logger, X509Certificate certificate, IMemoryStreamFactory memoryStreamProvider, ITextEncoding textEncoding, INetworkManager networkManager, ISocketFactory socketFactory, ICryptoProvider cryptoProvider, bool enableDualMode, IFileSystem fileSystem, IEnvironmentInfo environment)
        {
            _logger = logger;
            _certificate = certificate;
            _memoryStreamProvider = memoryStreamProvider;
            _textEncoding = textEncoding;
            _networkManager = networkManager;
            _socketFactory = socketFactory;
            _cryptoProvider = cryptoProvider;
            _enableDualMode = enableDualMode;
            _fileSystem = fileSystem;
            _environment = environment;

            _disposeCancellationToken = _disposeCancellationTokenSource.Token;
        }

        public Action<Exception, IRequest, bool> ErrorHandler { get; set; }
        public Func<IHttpRequest, string, string, string, CancellationToken, Task> RequestHandler { get; set; }

        public Action<WebSocketConnectingEventArgs> WebSocketConnecting { get; set; }

        public Action<WebSocketConnectEventArgs> WebSocketConnected { get; set; }

        public void Start(IEnumerable<string> urlPrefixes)
        {
            if (_listener == null)
                _listener = new HttpListener(_logger, _cryptoProvider, _socketFactory, _networkManager, _textEncoding, _memoryStreamProvider, _fileSystem, _environment);

            _listener.EnableDualMode = _enableDualMode;

            if (_certificate != null)
            {
                _listener.LoadCert(_certificate);
            }

            foreach (var prefix in urlPrefixes)
            {
                _logger.Info("Adding HttpListener prefix " + prefix);
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

        private Task InitTask(HttpListenerContext context, CancellationToken cancellationToken)
        {
            IHttpRequest httpReq = null;
            var request = context.Request;

            try
            {
                if (request.IsWebSocketRequest)
                {
                    LoggerUtils.LogRequest(_logger, request);

                    ProcessWebSocketRequest(context);
                    return Task.FromResult(true);
                }

                httpReq = GetRequest(context);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error processing request", ex);

                httpReq = httpReq ?? GetRequest(context);
                ErrorHandler(ex, httpReq, true);
                return Task.FromResult(true);
            }

            var uri = request.Url;

            return RequestHandler(httpReq, uri.OriginalString, uri.Host, uri.LocalPath, cancellationToken);
        }

        private void ProcessWebSocketRequest(HttpListenerContext ctx)
        {
            try
            {
                var endpoint = ctx.Request.RemoteEndPoint.ToString();
                var url = ctx.Request.RawUrl;

                var connectingArgs = new WebSocketConnectingEventArgs
                {
                    Url = url,
                    QueryString = ctx.Request.QueryString,
                    Endpoint = endpoint
                };

                if (WebSocketConnecting != null)
                {
                    WebSocketConnecting(connectingArgs);
                }

                if (connectingArgs.AllowConnection)
                {
                    _logger.Debug("Web socket connection allowed");

                    var webSocketContext = ctx.AcceptWebSocket(null);

                    if (WebSocketConnected != null)
                    {
                        WebSocketConnected(new WebSocketConnectEventArgs
                        {
                            Url = url,
                            QueryString = ctx.Request.QueryString,
                            WebSocket = new SharpWebSocket(webSocketContext.WebSocket, _logger),
                            Endpoint = endpoint
                        });
                    }
                }
                else
                {
                    _logger.Warn("Web socket connection not allowed");
                    ctx.Response.StatusCode = 401;
                    ctx.Response.Close();
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

            var req = new WebSocketSharpRequest(httpContext, operationName, _logger, _memoryStreamProvider);

            return req;
        }

        public Task Stop()
        {
            _disposeCancellationTokenSource.Cancel();

            if (_listener != null)
            {
                _listener.Close();
            }

            return Task.FromResult(true);
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