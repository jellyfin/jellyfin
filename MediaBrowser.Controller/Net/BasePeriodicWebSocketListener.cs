#nullable disable

#pragma warning disable CS1591, SA1306, SA1401

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Starts sending data over a web socket periodically when a message is received, and then stops when a corresponding stop message is received.
    /// </summary>
    /// <typeparam name="TReturnDataType">The type of the T return data type.</typeparam>
    /// <typeparam name="TStateType">The type of the T state type.</typeparam>
    public abstract class BasePeriodicWebSocketListener<TReturnDataType, TStateType> : IWebSocketListener, IAsyncDisposable
        where TStateType : WebSocketListenerState, new()
        where TReturnDataType : class
    {
        private readonly Channel<bool> _channel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

        private readonly object _activeConnectionsLock = new();

        /// <summary>
        /// The _active connections.
        /// </summary>
        private readonly List<(IWebSocketConnection Connection, CancellationTokenSource CancellationTokenSource, TStateType State)> _activeConnections = new();

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger<BasePeriodicWebSocketListener<TReturnDataType, TStateType>> Logger;

        private readonly Task _messageConsumerTask;

        protected BasePeriodicWebSocketListener(ILogger<BasePeriodicWebSocketListener<TReturnDataType, TStateType>> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            Logger = logger;

            _messageConsumerTask = HandleMessages();
        }

        /// <summary>
        /// Gets the type used for the messages sent to the client.
        /// </summary>
        /// <value>The type.</value>
        protected abstract SessionMessageType Type { get; }

        /// <summary>
        /// Gets the message type received from the client to start sending messages.
        /// </summary>
        /// <value>The type.</value>
        protected abstract SessionMessageType StartType { get; }

        /// <summary>
        /// Gets the message type received from the client to stop sending messages.
        /// </summary>
        /// <value>The type.</value>
        protected abstract SessionMessageType StopType { get; }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <returns>Task{`1}.</returns>
        protected abstract Task<TReturnDataType> GetDataToSend();

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task ProcessMessageAsync(WebSocketMessageInfo message)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (message.MessageType == StartType)
            {
                Start(message);
            }

            if (message.MessageType == StopType)
            {
                Stop(message);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ProcessWebSocketConnectedAsync(IWebSocketConnection connection, HttpContext httpContext) => Task.CompletedTask;

        /// <summary>
        /// Starts sending messages over a web socket.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual void Start(WebSocketMessageInfo message)
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

            lock (_activeConnectionsLock)
            {
                _activeConnections.Add((message.Connection, cancellationTokenSource, state));
            }
        }

        protected void SendData(bool force)
        {
            _channel.Writer.TryWrite(force);
        }

        private async Task HandleMessages()
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var force))
                {
                    try
                    {
                        (IWebSocketConnection Connection, CancellationTokenSource CancellationTokenSource, TStateType State)[] tuples;

                        var now = DateTime.UtcNow;
                        lock (_activeConnectionsLock)
                        {
                            if (_activeConnections.Count == 0)
                            {
                                continue;
                            }

                            tuples = _activeConnections
                                .Where(c =>
                                {
                                    if (c.Connection.State != WebSocketState.Open || c.CancellationTokenSource.IsCancellationRequested)
                                    {
                                        return false;
                                    }

                                    var state = c.State;
                                    return force || (now - state.DateLastSendUtc).TotalMilliseconds >= state.IntervalMs;
                                })
                                .ToArray();
                        }

                        if (tuples.Length == 0)
                        {
                            continue;
                        }

                        var data = await GetDataToSend().ConfigureAwait(false);
                        if (data is null)
                        {
                            continue;
                        }

                        IEnumerable<Task> GetTasks()
                        {
                            foreach (var tuple in tuples)
                            {
                                yield return SendDataInternal(data, tuple);
                            }
                        }

                        await Task.WhenAll(GetTasks()).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to send updates to websockets");
                    }
                }
            }
        }

        private async Task SendDataInternal(TReturnDataType data, (IWebSocketConnection Connection, CancellationTokenSource CancellationTokenSource, TStateType State) tuple)
        {
            try
            {
                var (connection, cts, state) = tuple;
                var cancellationToken = cts.Token;
                await connection.SendAsync(
                    new OutboundWebSocketMessage<TReturnDataType> { MessageType = Type, Data = data },
                    cancellationToken).ConfigureAwait(false);

                state.DateLastSendUtc = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                if (tuple.CancellationTokenSource.IsCancellationRequested)
                {
                    DisposeConnection(tuple);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error sending web socket message {Name}", Type);
                DisposeConnection(tuple);
            }
        }

        /// <summary>
        /// Stops sending messages over a web socket.
        /// </summary>
        /// <param name="message">The message.</param>
        private void Stop(WebSocketMessageInfo message)
        {
            lock (_activeConnectionsLock)
            {
                var connection = _activeConnections.FirstOrDefault(c => c.Connection == message.Connection);

                if (connection != default)
                {
                    DisposeConnection(connection);
                }
            }
        }

        /// <summary>
        /// Disposes the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void DisposeConnection((IWebSocketConnection Connection, CancellationTokenSource CancellationTokenSource, TStateType State) connection)
        {
            Logger.LogDebug("WS {1} stop transmitting to {0}", connection.Connection.RemoteEndPoint, GetType().Name);

            // TODO disposing the connection seems to break websockets in subtle ways, so what is the purpose of this function really...
            // connection.Item1.Dispose();

            try
            {
                connection.CancellationTokenSource.Cancel();
                connection.CancellationTokenSource.Dispose();
            }
            catch (ObjectDisposedException ex)
            {
                // TODO Investigate and properly fix.
                Logger.LogError(ex, "Object Disposed");
            }
            catch (Exception ex)
            {
                // TODO Investigate and properly fix.
                Logger.LogError(ex, "Error disposing websocket");
            }

            lock (_activeConnectionsLock)
            {
                _activeConnections.Remove(connection);
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            try
            {
                _channel.Writer.TryComplete();
                await _messageConsumerTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Disposing the message consumer failed");
            }

            lock (_activeConnectionsLock)
            {
                foreach (var connection in _activeConnections.ToList())
                {
                    DisposeConnection(connection);
                }
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
