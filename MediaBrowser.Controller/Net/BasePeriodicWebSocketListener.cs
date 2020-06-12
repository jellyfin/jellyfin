using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Starts sending data over a web socket periodically when a message is received, and then stops when a corresponding stop message is received
    /// </summary>
    /// <typeparam name="TReturnDataType">The type of the T return data type.</typeparam>
    /// <typeparam name="TStateType">The type of the T state type.</typeparam>
    public abstract class BasePeriodicWebSocketListener<TReturnDataType, TStateType> : IWebSocketListener, IDisposable
        where TStateType : WebSocketListenerState, new()
        where TReturnDataType : class
    {
        /// <summary>
        /// The _active connections
        /// </summary>
        private readonly List<Tuple<IWebSocketConnection, CancellationTokenSource, TStateType>> _activeConnections =
            new List<Tuple<IWebSocketConnection, CancellationTokenSource, TStateType>>();

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected abstract string Name { get; }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <returns>Task{`1}.</returns>
        protected abstract Task<TReturnDataType> GetDataToSend();

        /// <summary>
        /// The logger
        /// </summary>
        protected ILogger Logger;

        protected BasePeriodicWebSocketListener(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Logger = logger;
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task ProcessMessageAsync(WebSocketMessageInfo message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.Equals(message.MessageType, Name + "Start", StringComparison.OrdinalIgnoreCase))
            {
                Start(message);
            }

            if (string.Equals(message.MessageType, Name + "Stop", StringComparison.OrdinalIgnoreCase))
            {
                Stop(message);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts sending messages over a web socket
        /// </summary>
        /// <param name="message">The message.</param>
        private void Start(WebSocketMessageInfo message)
        {
            var vals = message.Data.Split(',');

            var dueTimeMs = long.Parse(vals[0], CultureInfo.InvariantCulture);
            var periodMs = long.Parse(vals[1], CultureInfo.InvariantCulture);

            var cancellationTokenSource = new CancellationTokenSource();

            Logger.LogDebug("WS {1} begin transmitting to {0}", message.Connection.RemoteEndPoint, GetType().Name);

            var state = new TStateType
            {
                IntervalMs = periodMs,
                InitialDelayMs = dueTimeMs
            };

            lock (_activeConnections)
            {
                _activeConnections.Add(new Tuple<IWebSocketConnection, CancellationTokenSource, TStateType>(message.Connection, cancellationTokenSource, state));
            }
        }

        protected async Task SendData(bool force)
        {
            Tuple<IWebSocketConnection, CancellationTokenSource, TStateType>[] tuples;

            lock (_activeConnections)
            {
                tuples = _activeConnections
                    .Where(c =>
                    {
                        if (c.Item1.State == WebSocketState.Open && !c.Item2.IsCancellationRequested)
                        {
                            var state = c.Item3;

                            if (force || (DateTime.UtcNow - state.DateLastSendUtc).TotalMilliseconds >= state.IntervalMs)
                            {
                                return true;
                            }
                        }

                        return false;
                    })
                    .ToArray();
            }

            IEnumerable<Task> GetTasks()
            {
                foreach (var tuple in tuples)
                {
                    yield return SendData(tuple);
                }
            }

            await Task.WhenAll(GetTasks()).ConfigureAwait(false);
        }

        private async Task SendData(Tuple<IWebSocketConnection, CancellationTokenSource, TStateType> tuple)
        {
            var connection = tuple.Item1;

            try
            {
                var state = tuple.Item3;

                var cancellationToken = tuple.Item2.Token;

                var data = await GetDataToSend().ConfigureAwait(false);

                if (data != null)
                {
                    await connection.SendAsync(
                        new WebSocketMessage<TReturnDataType>
                        {
                            MessageType = Name,
                            Data = data
                        },
                        cancellationToken).ConfigureAwait(false);

                    state.DateLastSendUtc = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException)
            {
                if (tuple.Item2.IsCancellationRequested)
                {
                    DisposeConnection(tuple);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error sending web socket message {Name}", Name);
                DisposeConnection(tuple);
            }
        }

        /// <summary>
        /// Stops sending messages over a web socket
        /// </summary>
        /// <param name="message">The message.</param>
        private void Stop(WebSocketMessageInfo message)
        {
            lock (_activeConnections)
            {
                var connection = _activeConnections.FirstOrDefault(c => c.Item1 == message.Connection);

                if (connection != null)
                {
                    DisposeConnection(connection);
                }
            }
        }

        /// <summary>
        /// Disposes the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void DisposeConnection(Tuple<IWebSocketConnection, CancellationTokenSource, TStateType> connection)
        {
            Logger.LogDebug("WS {1} stop transmitting to {0}", connection.Item1.RemoteEndPoint, GetType().Name);

            // TODO disposing the connection seems to break websockets in subtle ways, so what is the purpose of this function really...
            // connection.Item1.Dispose();

            try
            {
                connection.Item2.Cancel();
                connection.Item2.Dispose();
            }
            catch (ObjectDisposedException)
            {
                //TODO Investigate and properly fix.
            }

            lock (_activeConnections)
            {
                _activeConnections.Remove(connection);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                lock (_activeConnections)
                {
                    foreach (var connection in _activeConnections.ToArray())
                    {
                        DisposeConnection(connection);
                    }
                }
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
    }

    public class WebSocketListenerState
    {
        public DateTime DateLastSendUtc { get; set; }
        public long InitialDelayMs { get; set; }
        public long IntervalMs { get; set; }
    }
}
