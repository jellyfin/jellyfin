using MediaBrowser.Common.Events;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.IO;

namespace MediaBrowser.Server.Implementations.ServerManager
{
    /// <summary>
    /// Manages the Http Server, Udp Server and WebSocket connections
    /// </summary>
    public class ServerManager : IServerManager
    {
        /// <summary>
        /// Both the Ui and server will have a built-in HttpServer.
        /// People will inevitably want remote control apps so it's needed in the Ui too.
        /// </summary>
        /// <value>The HTTP server.</value>
        private IHttpServer HttpServer { get; set; }

        /// <summary>
        /// Gets or sets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The web socket connections
        /// </summary>
        private readonly List<IWebSocketConnection> _webSocketConnections = new List<IWebSocketConnection>();
        /// <summary>
        /// Gets the web socket connections.
        /// </summary>
        /// <value>The web socket connections.</value>
        public IEnumerable<IWebSocketConnection> WebSocketConnections
        {
            get { return _webSocketConnections; }
        }

        public event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _application host
        /// </summary>
        private readonly IServerApplicationHost _applicationHost;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IServerConfigurationManager ConfigurationManager { get; set; }

        /// <summary>
        /// Gets the web socket listeners.
        /// </summary>
        /// <value>The web socket listeners.</value>
        private readonly List<IWebSocketListener> _webSocketListeners = new List<IWebSocketListener>();

        private bool _disposed;
        private readonly IMemoryStreamProvider _memoryStreamProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerManager" /> class.
        /// </summary>
        /// <param name="applicationHost">The application host.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <exception cref="System.ArgumentNullException">applicationHost</exception>
        public ServerManager(IServerApplicationHost applicationHost, IJsonSerializer jsonSerializer, ILogger logger, IServerConfigurationManager configurationManager, IMemoryStreamProvider memoryStreamProvider)
        {
            if (applicationHost == null)
            {
                throw new ArgumentNullException("applicationHost");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _applicationHost = applicationHost;
            ConfigurationManager = configurationManager;
            _memoryStreamProvider = memoryStreamProvider;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start(IEnumerable<string> urlPrefixes, string certificatePath)
        {
            ReloadHttpServer(urlPrefixes, certificatePath);
        }

        /// <summary>
        /// Restarts the Http Server, or starts it if not currently running
        /// </summary>
        private void ReloadHttpServer(IEnumerable<string> urlPrefixes, string certificatePath)
        {
            _logger.Info("Loading Http Server");

            try
            {
                HttpServer = _applicationHost.Resolve<IHttpServer>();
                HttpServer.StartServer(urlPrefixes, certificatePath);
            }
            catch (SocketException ex)
            {
                _logger.ErrorException("The http server is unable to start due to a Socket error. This can occasionally happen when the operating system takes longer than usual to release the IP bindings from the previous session. This can take up to five minutes. Please try waiting or rebooting the system.", ex);

                throw;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error starting Http Server", ex);

                throw;
            }

            HttpServer.WebSocketConnected += HttpServer_WebSocketConnected;
        }

        /// <summary>
        /// Handles the WebSocketConnected event of the HttpServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="WebSocketConnectEventArgs" /> instance containing the event data.</param>
        void HttpServer_WebSocketConnected(object sender, WebSocketConnectEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            var connection = new WebSocketConnection(e.WebSocket, e.Endpoint, _jsonSerializer, _logger, _memoryStreamProvider)
            {
                OnReceive = ProcessWebSocketMessageReceived,
                Url = e.Url,
                QueryString = new NameValueCollection(e.QueryString ?? new NameValueCollection())
            };

            _webSocketConnections.Add(connection);

            if (WebSocketConnected != null)
            {
                EventHelper.FireEventIfNotNull(WebSocketConnected, this, new GenericEventArgs<IWebSocketConnection> (connection), _logger);
            }
        }

        /// <summary>
        /// Processes the web socket message received.
        /// </summary>
        /// <param name="result">The result.</param>
        private async void ProcessWebSocketMessageReceived(WebSocketMessageInfo result)
        {
            if (_disposed)
            {
                return;
            }

            //_logger.Debug("Websocket message received: {0}", result.MessageType);

            var tasks = _webSocketListeners.Select(i => Task.Run(async () =>
            {
                try
                {
                    await i.ProcessMessage(result).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("{0} failed processing WebSocket message {1}", ex, i.GetType().Name, result.MessageType ?? string.Empty);
                }
            }));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a message to all clients currently connected via a web socket
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        public void SendWebSocketMessage<T>(string messageType, T data)
        {
            SendWebSocketMessage(messageType, () => data);
        }

        /// <summary>
        /// Sends a message to all clients currently connected via a web socket
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="dataFunction">The function that generates the data to send, if there are any connected clients</param>
        public void SendWebSocketMessage<T>(string messageType, Func<T> dataFunction)
        {
            SendWebSocketMessageAsync(messageType, dataFunction, CancellationToken.None);
        }

        /// <summary>
        /// Sends a message to all clients currently connected via a web socket
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="dataFunction">The function that generates the data to send, if there are any connected clients</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">messageType</exception>
        public Task SendWebSocketMessageAsync<T>(string messageType, Func<T> dataFunction, CancellationToken cancellationToken)
        {
            return SendWebSocketMessageAsync(messageType, dataFunction, _webSocketConnections, cancellationToken);
        }

        /// <summary>
        /// Sends the web socket message async.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="dataFunction">The data function.</param>
        /// <param name="connections">The connections.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">messageType
        /// or
        /// dataFunction
        /// or
        /// cancellationToken</exception>
        private async Task SendWebSocketMessageAsync<T>(string messageType, Func<T> dataFunction, IEnumerable<IWebSocketConnection> connections, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(messageType))
            {
                throw new ArgumentNullException("messageType");
            }

            if (dataFunction == null)
            {
                throw new ArgumentNullException("dataFunction");
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var connectionsList = connections.Where(s => s.State == WebSocketState.Open).ToList();

            if (connectionsList.Count > 0)
            {
                _logger.Info("Sending web socket message {0}", messageType);

                var message = new WebSocketMessage<T> { MessageType = messageType, Data = dataFunction() };
                var json = _jsonSerializer.SerializeToString(message);

                var tasks = connectionsList.Select(s => Task.Run(() =>
                {
                    try
                    {
                        s.SendAsync(json, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error sending web socket message {0} to {1}", ex, messageType, s.RemoteEndPoint);
                    }

                }, cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disposes the current HttpServer
        /// </summary>
        private void DisposeHttpServer()
        {
            foreach (var socket in _webSocketConnections)
            {
                // Dispose the connection
                socket.Dispose();
            }

            _webSocketConnections.Clear();

            if (HttpServer != null)
            {
                HttpServer.WebSocketConnected -= HttpServer_WebSocketConnected;
                HttpServer.Dispose();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                DisposeHttpServer();
            }
        }

        /// <summary>
        /// Adds the web socket listeners.
        /// </summary>
        /// <param name="listeners">The listeners.</param>
        public void AddWebSocketListeners(IEnumerable<IWebSocketListener> listeners)
        {
            _webSocketListeners.AddRange(listeners);
        }
    }
}
