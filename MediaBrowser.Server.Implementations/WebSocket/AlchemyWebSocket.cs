using Alchemy.Classes;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.WebSocket
{
    /// <summary>
    /// Class AlchemyWebSocket
    /// </summary>
    public class AlchemyWebSocket : IWebSocket
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        /// <value>The web socket.</value>
        private UserContext UserContext { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlchemyWebSocket" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">context</exception>
        public AlchemyWebSocket(UserContext context, ILogger logger)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            _logger = logger;
            UserContext = context;

            context.SetOnDisconnect(OnDisconnected);
            context.SetOnReceive(OnReceiveContext);

            _logger.Info("Client connected from {0}", context.ClientAddress);
        }

        /// <summary>
        /// The _disconnected
        /// </summary>
        private bool _disconnected;
        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State
        {
            get { return _disconnected ? WebSocketState.Closed : WebSocketState.Open; }
        }

        /// <summary>
        /// Called when [disconnected].
        /// </summary>
        /// <param name="context">The context.</param>
        private void OnDisconnected(UserContext context)
        {
            _disconnected = true;
        }

        /// <summary>
        /// Called when [receive].
        /// </summary>
        /// <param name="context">The context.</param>
        private void OnReceiveContext(UserContext context)
        {
            if (OnReceive != null)
            {
                var json = context.DataFrame.ToString();

                OnReceive(json);
            }
        }
        
        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="type">The type.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(byte[] bytes, WebSocketMessageType type, bool endOfMessage, CancellationToken cancellationToken)
        {
            return Task.Run(() => UserContext.Send(bytes));
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
