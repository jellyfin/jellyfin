using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Manages the Http Server, Udp Server and WebSocket connections
    /// </summary>
    public class TcpManager : IDisposable
    {
        /// <summary>
        /// This is the udp server used for server discovery by clients
        /// </summary>
        /// <value>The UDP server.</value>
        private IUdpServer UdpServer { get; set; }

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
        /// This subscribes to HttpListener requests and finds the appropriate BaseHandler to process it
        /// </summary>
        /// <value>The HTTP listener.</value>
        private IDisposable HttpListener { get; set; }

        /// <summary>
        /// The web socket connections
        /// </summary>
        private readonly List<WebSocketConnection> _webSocketConnections = new List<WebSocketConnection>();

        /// <summary>
        /// Gets or sets the external web socket server.
        /// </summary>
        /// <value>The external web socket server.</value>
        private IWebSocketServer ExternalWebSocketServer { get; set; }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _network manager
        /// </summary>
        private readonly INetworkManager _networkManager;

        /// <summary>
        /// The _application host
        /// </summary>
        private readonly IApplicationHost _applicationHost;

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly IKernel _kernel;

        /// <summary>
        /// Gets a value indicating whether [supports web socket].
        /// </summary>
        /// <value><c>true</c> if [supports web socket]; otherwise, <c>false</c>.</value>
        internal bool SupportsNativeWebSocket
        {
            get { return HttpServer != null && HttpServer.SupportsWebSockets; }
        }

        /// <summary>
        /// Gets the web socket port number.
        /// </summary>
        /// <value>The web socket port number.</value>
        public int WebSocketPortNumber
        {
            get { return SupportsNativeWebSocket ? _kernel.Configuration.HttpServerPortNumber : _kernel.Configuration.LegacyWebSocketPortNumber; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpManager" /> class.
        /// </summary>
        /// <param name="applicationHost">The application host.</param>
        /// <param name="kernel">The kernel.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">applicationHost</exception>
        public TcpManager(IApplicationHost applicationHost, IKernel kernel, INetworkManager networkManager, IJsonSerializer jsonSerializer, ILogger logger)
        {
            if (applicationHost == null)
            {
                throw new ArgumentNullException("applicationHost");
            }
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }
            if (networkManager == null)
            {
                throw new ArgumentNullException("networkManager");
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
            _kernel = kernel;
            _applicationHost = applicationHost;
            _networkManager = networkManager;

            if (kernel.IsFirstRun)
            {
                RegisterServerWithAdministratorAccess();
            }

            ReloadUdpServer();
            ReloadHttpServer();

            if (!SupportsNativeWebSocket)
            {
                ReloadExternalWebSocketServer();
            }
        }

        /// <summary>
        /// Starts the external web socket server.
        /// </summary>
        private void ReloadExternalWebSocketServer()
        {
            // Avoid windows firewall prompts in the ui
            if (_kernel.KernelContext != KernelContext.Server)
            {
                return;
            }

            DisposeExternalWebSocketServer();

            ExternalWebSocketServer = _applicationHost.Resolve<IWebSocketServer>();

            ExternalWebSocketServer.Start(_kernel.Configuration.LegacyWebSocketPortNumber);
            ExternalWebSocketServer.WebSocketConnected += HttpServer_WebSocketConnected;
        }

        /// <summary>
        /// Restarts the Http Server, or starts it if not currently running
        /// </summary>
        /// <param name="registerServerOnFailure">if set to <c>true</c> [register server on failure].</param>
        public void ReloadHttpServer(bool registerServerOnFailure = true)
        {
            // Only reload if the port has changed, so that we don't disconnect any active users
            if (HttpServer != null && HttpServer.UrlPrefix.Equals(_kernel.HttpServerUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            DisposeHttpServer();

            _logger.Info("Loading Http Server");

            try
            {
                HttpServer = _applicationHost.Resolve<IHttpServer>();
                HttpServer.EnableHttpRequestLogging = _kernel.Configuration.EnableHttpLevelLogging;
                HttpServer.Start(_kernel.HttpServerUrlPrefix);
            }
            catch (HttpListenerException ex)
            {
                _logger.ErrorException("Error starting Http Server", ex);

                if (registerServerOnFailure)
                {
                    RegisterServerWithAdministratorAccess();

                    // Don't get stuck in a loop
                    ReloadHttpServer(false);

                    return;
                }

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
            var connection = new WebSocketConnection(e.WebSocket, e.Endpoint, ProcessWebSocketMessageReceived, _jsonSerializer, _logger);

            _webSocketConnections.Add(connection);
        }

        /// <summary>
        /// Processes the web socket message received.
        /// </summary>
        /// <param name="result">The result.</param>
        private async void ProcessWebSocketMessageReceived(WebSocketMessageInfo result)
        {
            var tasks = _kernel.WebSocketListeners.Select(i => Task.Run(async () =>
            {
                try
                {
                    await i.ProcessMessage(result).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("{0} failed processing WebSocket message {1}", ex, i.GetType().Name, result.MessageType);
                }
            }));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts or re-starts the udp server
        /// </summary>
        private void ReloadUdpServer()
        {
            // For now, there's no reason to keep reloading this over and over
            if (UdpServer != null)
            {
                return;
            }

            // Avoid windows firewall prompts in the ui
            if (_kernel.KernelContext != KernelContext.Server)
            {
                return;
            }

            DisposeUdpServer();

            try
            {
                // The port number can't be in configuration because we don't want it to ever change
                UdpServer = _applicationHost.Resolve<IUdpServer>();
                UdpServer.Start(_kernel.UdpServerPortNumber);
            }
            catch (SocketException ex)
            {
                _logger.ErrorException("Failed to start UDP Server", ex);
                return;
            }

            UdpServer.MessageReceived += UdpServer_MessageReceived;
        }

        /// <summary>
        /// Handles the MessageReceived event of the UdpServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="UdpMessageReceivedEventArgs" /> instance containing the event data.</param>
        async void UdpServer_MessageReceived(object sender, UdpMessageReceivedEventArgs e)
        {
            var expectedMessage = String.Format("who is MediaBrowser{0}?", _kernel.KernelContext);
            var expectedMessageBytes = Encoding.UTF8.GetBytes(expectedMessage);

            if (expectedMessageBytes.SequenceEqual(e.Bytes))
            {
                _logger.Info("Received UDP server request from " + e.RemoteEndPoint);

                // Send a response back with our ip address and port
                var response = String.Format("MediaBrowser{0}|{1}:{2}", _kernel.KernelContext, _networkManager.GetLocalIpAddress(), _kernel.UdpServerPortNumber);

                await UdpServer.SendAsync(Encoding.UTF8.GetBytes(response), e.RemoteEndPoint);
            }
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
            Task.Run(async () => await SendWebSocketMessageAsync(messageType, dataFunction, CancellationToken.None).ConfigureAwait(false));
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
        public async Task SendWebSocketMessageAsync<T>(string messageType, Func<T> dataFunction, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(messageType))
            {
                throw new ArgumentNullException("messageType");
            }

            if (dataFunction == null)
            {
                throw new ArgumentNullException("dataFunction");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var connections = _webSocketConnections.Where(s => s.State == WebSocketState.Open).ToList();

            if (connections.Count > 0)
            {
                _logger.Info("Sending web socket message {0}", messageType);

                var message = new WebSocketMessage<T> { MessageType = messageType, Data = dataFunction() };
                var bytes = _jsonSerializer.SerializeToBytes(message);

                var tasks = connections.Select(s => Task.Run(() =>
                {
                    try
                    {
                        s.SendAsync(bytes, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error sending web socket message {0} to {1}", ex, messageType, s.RemoteEndPoint);
                    }
                }));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disposes the udp server
        /// </summary>
        private void DisposeUdpServer()
        {
            if (UdpServer != null)
            {
                UdpServer.MessageReceived -= UdpServer_MessageReceived;
                UdpServer.Dispose();
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
                _logger.Info("Disposing Http Server");

                HttpServer.WebSocketConnected -= HttpServer_WebSocketConnected;
                HttpServer.Dispose();
            }

            if (HttpListener != null)
            {
                HttpListener.Dispose();
            }

            DisposeExternalWebSocketServer();
        }

        /// <summary>
        /// Registers the server with administrator access.
        /// </summary>
        private void RegisterServerWithAdministratorAccess()
        {
            // Create a temp file path to extract the bat file to
            var tmpFile = Path.Combine(_kernel.ApplicationPaths.TempDirectory, Guid.NewGuid() + ".bat");

            // Extract the bat file
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.Common.Kernel.RegisterServer.bat"))
            {
                using (var fileStream = File.Create(tmpFile))
                {
                    stream.CopyTo(fileStream);
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = tmpFile,

                Arguments = string.Format("{0} {1} {2} {3}", _kernel.Configuration.HttpServerPortNumber,
                _kernel.HttpServerUrlPrefix,
                _kernel.UdpServerPortNumber,
                _kernel.Configuration.LegacyWebSocketPortNumber),

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
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
                DisposeUdpServer();
                DisposeHttpServer();
            }
        }

        /// <summary>
        /// Disposes the external web socket server.
        /// </summary>
        private void DisposeExternalWebSocketServer()
        {
            if (ExternalWebSocketServer != null)
            {
                ExternalWebSocketServer.Dispose();
            }
        }

        /// <summary>
        /// Called when [application configuration changed].
        /// </summary>
        /// <param name="oldConfig">The old config.</param>
        /// <param name="newConfig">The new config.</param>
        public void OnApplicationConfigurationChanged(BaseApplicationConfiguration oldConfig, BaseApplicationConfiguration newConfig)
        {
            HttpServer.EnableHttpRequestLogging = newConfig.EnableHttpLevelLogging;

            if (oldConfig.HttpServerPortNumber != newConfig.HttpServerPortNumber)
            {
                ReloadHttpServer();
            }

            if (!SupportsNativeWebSocket && oldConfig.LegacyWebSocketPortNumber != newConfig.LegacyWebSocketPortNumber)
            {
                ReloadExternalWebSocketServer();
            }
        }
    }
}
