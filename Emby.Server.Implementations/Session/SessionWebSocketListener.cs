using System;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionWebSocketListener
    /// </summary>
    public sealed class SessionWebSocketListener : IWebSocketListener, IDisposable
    {
        /// <summary>
        /// The _session manager
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly IHttpServer _httpServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWebSocketListener" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="httpServer">The HTTP server.</param>
        public SessionWebSocketListener(
            ILogger<SessionWebSocketListener> logger,
            ISessionManager sessionManager,
            ILoggerFactory loggerFactory,
            IHttpServer httpServer)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _loggerFactory = loggerFactory;
            _httpServer = httpServer;

            httpServer.WebSocketConnected += OnServerManagerWebSocketConnected;
        }

        private void OnServerManagerWebSocketConnected(object sender, GenericEventArgs<IWebSocketConnection> e)
        {
            var session = GetSession(e.Argument.QueryString, e.Argument.RemoteEndPoint.ToString());
            if (session != null)
            {
                EnsureController(session, e.Argument);
            }
            else
            {
                _logger.LogWarning("Unable to determine session based on query string: {0}", e.Argument.QueryString);
            }
        }

        private SessionInfo GetSession(IQueryCollection queryString, string remoteEndpoint)
        {
            if (queryString == null)
            {
                return null;
            }

            var token = queryString["api_key"];
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var deviceId = queryString["deviceId"];
            return _sessionManager.GetSessionByAuthenticationToken(token, deviceId, remoteEndpoint);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _httpServer.WebSocketConnected -= OnServerManagerWebSocketConnected;
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task ProcessMessageAsync(WebSocketMessageInfo message)
            => Task.CompletedTask;

        private void EnsureController(SessionInfo session, IWebSocketConnection connection)
        {
            var controllerInfo = session.EnsureController<WebSocketController>(
                s => new WebSocketController(_loggerFactory.CreateLogger<WebSocketController>(), s, _sessionManager));

            var controller = (WebSocketController)controllerInfo.Item1;
            controller.AddWebSocket(connection);
        }
    }
}
