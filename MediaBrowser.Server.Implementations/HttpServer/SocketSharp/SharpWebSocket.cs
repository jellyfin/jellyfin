using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketState = MediaBrowser.Model.Net.WebSocketState;

namespace MediaBrowser.Server.Implementations.HttpServer.SocketSharp
{
    public class SharpWebSocket : IWebSocket
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        public event EventHandler<EventArgs> Closed;

        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        /// <value>The web socket.</value>
        private SocketHttpListener.WebSocket WebSocket { get; set; }

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeWebSocket" /> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">socket</exception>
        public SharpWebSocket(SocketHttpListener.WebSocket socket, ILogger logger)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
            WebSocket = socket;

            socket.OnMessage += socket_OnMessage;
            socket.OnClose += socket_OnClose;
            socket.OnError += socket_OnError;

            WebSocket.ConnectAsServer();
        }

        void socket_OnError(object sender, SocketHttpListener.ErrorEventArgs e)
        {
            _logger.Error("Error in SharpWebSocket: {0}", e.Message ?? string.Empty);
            //EventHelper.FireEventIfNotNull(Closed, this, EventArgs.Empty, _logger);
        }

        void socket_OnClose(object sender, SocketHttpListener.CloseEventArgs e)
        {
            EventHelper.FireEventIfNotNull(Closed, this, EventArgs.Empty, _logger);
        }

        void socket_OnMessage(object sender, SocketHttpListener.MessageEventArgs e)
        {
            //if (!string.IsNullOrWhiteSpace(e.Data))
            //{
            //    if (OnReceive != null)
            //    {
            //        OnReceive(e.Data);
            //    }
            //    return;
            //}
            if (OnReceiveBytes != null)
            {
                OnReceiveBytes(e.RawData);
            }
        }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State
        {
            get
            {
                WebSocketState commonState;

                if (!Enum.TryParse(WebSocket.ReadyState.ToString(), true, out commonState))
                {
                    _logger.Warn("Unrecognized WebSocketState: {0}", WebSocket.ReadyState.ToString());
                }

                return commonState;
            }
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(byte[] bytes, bool endOfMessage, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<bool>();

            WebSocket.SendAsync(bytes, res => completionSource.TrySetResult(true));

            return completionSource.Task;
        }

        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(string text, bool endOfMessage, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<bool>();

            WebSocket.SendAsync(text, res => completionSource.TrySetResult(true));

            return completionSource.Task;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                WebSocket.OnMessage -= socket_OnMessage;
                WebSocket.OnClose -= socket_OnClose;
                WebSocket.OnError -= socket_OnError;

                _cancellationTokenSource.Cancel();

                WebSocket.Close();
            }
        }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        public Action<byte[]> OnReceiveBytes { get; set; }

        /// <summary>
        /// Gets or sets the on receive.
        /// </summary>
        /// <value>The on receive.</value>
        public Action<string> OnReceive { get; set; }
    }
}
