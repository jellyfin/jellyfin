using Alchemy;
using Alchemy.Classes;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Manages the Http Server, Udp Server and WebSocket connections
    /// </summary>
    public class TcpManager : BaseManager<IKernel>
    {
        /// <summary>
        /// This is the udp server used for server discovery by clients
        /// </summary>
        /// <value>The UDP server.</value>
        private UdpServer UdpServer { get; set; }

        /// <summary>
        /// Gets or sets the UDP listener.
        /// </summary>
        /// <value>The UDP listener.</value>
        private IDisposable UdpListener { get; set; }

        /// <summary>
        /// Both the Ui and server will have a built-in HttpServer.
        /// People will inevitably want remote control apps so it's needed in the Ui too.
        /// </summary>
        /// <value>The HTTP server.</value>
        public HttpServer HttpServer { get; private set; }

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
        private WebSocketServer ExternalWebSocketServer { get; set; }

        /// <summary>
        /// The _supports native web socket
        /// </summary>
        private bool? _supportsNativeWebSocket;

        /// <summary>
        /// Gets a value indicating whether [supports web socket].
        /// </summary>
        /// <value><c>true</c> if [supports web socket]; otherwise, <c>false</c>.</value>
        internal bool SupportsNativeWebSocket
        {
            get
            {
                if (!_supportsNativeWebSocket.HasValue)
                {
                    try
                    {
                        new ClientWebSocket();

                        _supportsNativeWebSocket = true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                        _supportsNativeWebSocket = false;
                    }
                }

                return _supportsNativeWebSocket.Value;
            }
        }

        /// <summary>
        /// Gets the web socket port number.
        /// </summary>
        /// <value>The web socket port number.</value>
        public int WebSocketPortNumber
        {
            get { return SupportsNativeWebSocket ? Kernel.Configuration.HttpServerPortNumber : Kernel.Configuration.LegacyWebSocketPortNumber; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public TcpManager(IKernel kernel)
            : base(kernel)
        {
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
            if (Kernel.KernelContext != KernelContext.Server)
            {
                return;
            }

            DisposeExternalWebSocketServer();

            ExternalWebSocketServer = new WebSocketServer(Kernel.Configuration.LegacyWebSocketPortNumber, IPAddress.Any)
            {
                OnConnected = OnAlchemyWebSocketClientConnected,
                TimeOut = TimeSpan.FromMinutes(60)
            };

            ExternalWebSocketServer.Start();

            Logger.Info("Alchemy Web Socket Server started");
        }

        /// <summary>
        /// Called when [alchemy web socket client connected].
        /// </summary>
        /// <param name="context">The context.</param>
        private void OnAlchemyWebSocketClientConnected(UserContext context)
        {
            var connection = new WebSocketConnection(new AlchemyWebSocket(context, Logger), context.ClientAddress, ProcessWebSocketMessageReceived, Logger);

            _webSocketConnections.Add(connection);
        }

        /// <summary>
        /// Restarts the Http Server, or starts it if not currently running
        /// </summary>
        /// <param name="registerServerOnFailure">if set to <c>true</c> [register server on failure].</param>
        public void ReloadHttpServer(bool registerServerOnFailure = true)
        {
            // Only reload if the port has changed, so that we don't disconnect any active users
            if (HttpServer != null && HttpServer.UrlPrefix.Equals(Kernel.HttpServerUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            DisposeHttpServer();

            Logger.Info("Loading Http Server");

            try
            {
                HttpServer = new HttpServer(Kernel.HttpServerUrlPrefix, "Media Browser", Kernel, Logger);
            }
            catch (HttpListenerException ex)
            {
                Logger.ErrorException("Error starting Http Server", ex);

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
            var connection = new WebSocketConnection(e.WebSocket, e.Endpoint, ProcessWebSocketMessageReceived, Logger);

            _webSocketConnections.Add(connection);
        }

        /// <summary>
        /// Processes the web socket message received.
        /// </summary>
        /// <param name="result">The result.</param>
        private async void ProcessWebSocketMessageReceived(WebSocketMessageInfo result)
        {
            var tasks = Kernel.WebSocketListeners.Select(i => Task.Run(async () =>
            {
                try
                {
                    await i.ProcessMessage(result).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("{0} failed processing WebSocket message {1}", ex, i.GetType().Name, result.MessageType);
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
            if (Kernel.KernelContext != KernelContext.Server)
            {
                return;
            }

            DisposeUdpServer();

            try
            {
                // The port number can't be in configuration because we don't want it to ever change
                UdpServer = new UdpServer(new IPEndPoint(IPAddress.Any, Kernel.UdpServerPortNumber));
            }
            catch (SocketException ex)
            {
                Logger.ErrorException("Failed to start UDP Server", ex);
                return;
            }

            UdpListener = UdpServer.Subscribe(async res =>
            {
                var expectedMessage = String.Format("who is MediaBrowser{0}?", Kernel.KernelContext);
                var expectedMessageBytes = Encoding.UTF8.GetBytes(expectedMessage);

                if (expectedMessageBytes.SequenceEqual(res.Buffer))
                {
                    Logger.Info("Received UDP server request from " + res.RemoteEndPoint.ToString());

                    // Send a response back with our ip address and port
                    var response = String.Format("MediaBrowser{0}|{1}:{2}", Kernel.KernelContext, NetUtils.GetLocalIpAddress(), Kernel.UdpServerPortNumber);

                    await UdpServer.SendAsync(response, res.RemoteEndPoint);
                }
            });
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
                Logger.Info("Sending web socket message {0}", messageType);

                var message = new WebSocketMessage<T> { MessageType = messageType, Data = dataFunction() };
                var bytes = JsonSerializer.SerializeToBytes(message);

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
                        Logger.ErrorException("Error sending web socket message {0} to {1}", ex, messageType, s.RemoteEndPoint);
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
                UdpServer.Dispose();
            }

            if (UdpListener != null)
            {
                UdpListener.Dispose();
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
                Logger.Info("Disposing Http Server");

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
            var tmpFile = Path.Combine(Kernel.ApplicationPaths.TempDirectory, Guid.NewGuid() + ".bat");

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

                Arguments = string.Format("{0} {1} {2} {3}", Kernel.Configuration.HttpServerPortNumber,
                Kernel.HttpServerUrlPrefix,
                Kernel.UdpServerPortNumber,
                Kernel.Configuration.LegacyWebSocketPortNumber),

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
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                DisposeUdpServer();
                DisposeHttpServer();
            }

            base.Dispose(dispose);
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
