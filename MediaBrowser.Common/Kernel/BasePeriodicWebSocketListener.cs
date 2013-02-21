using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Starts sending data over a web socket periodically when a message is received, and then stops when a corresponding stop message is received
    /// </summary>
    /// <typeparam name="TKernelType">The type of the T kernel type.</typeparam>
    /// <typeparam name="TReturnDataType">The type of the T return data type.</typeparam>
    /// <typeparam name="TStateType">The type of the T state type.</typeparam>
    public abstract class BasePeriodicWebSocketListener<TKernelType, TReturnDataType, TStateType> : BaseWebSocketListener<TKernelType>
        where TKernelType : IKernel
        where TStateType : class, new()
    {
        /// <summary>
        /// The _active connections
        /// </summary>
        protected readonly List<Tuple<WebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim>> ActiveConnections =
            new List<Tuple<WebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim>>();

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected abstract string Name { get; }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{`1}.</returns>
        protected abstract Task<TReturnDataType> GetDataToSend(TStateType state);

        /// <summary>
        /// Processes the message internal.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        protected override Task ProcessMessageInternal(WebSocketMessageInfo message)
        {
            if (message.MessageType.Equals(Name + "Start", StringComparison.OrdinalIgnoreCase))
            {
                Start(message);
            }

            if (message.MessageType.Equals(Name + "Stop", StringComparison.OrdinalIgnoreCase))
            {
                Stop(message);
            }

            return NullTaskResult;
        }

        /// <summary>
        /// Starts sending messages over a web socket
        /// </summary>
        /// <param name="message">The message.</param>
        private void Start(WebSocketMessageInfo message)
        {
            var vals = message.Data.Split(',');

            var dueTimeMs = long.Parse(vals[0]);
            var periodMs = long.Parse(vals[1]);

            var cancellationTokenSource = new CancellationTokenSource();

            Logger.LogInfo("{1} Begin transmitting over websocket to {0}", message.Connection.RemoteEndPoint, GetType().Name);

            var timer = new Timer(TimerCallback, message.Connection, Timeout.Infinite, Timeout.Infinite);

            var state = new TStateType();

            var semaphore = new SemaphoreSlim(1, 1);

            lock (ActiveConnections)
            {
                ActiveConnections.Add(new Tuple<WebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim>(message.Connection, cancellationTokenSource, timer, state, semaphore));
            }

            timer.Change(TimeSpan.FromMilliseconds(dueTimeMs), TimeSpan.FromMilliseconds(periodMs));
        }

        /// <summary>
        /// Timers the callback.
        /// </summary>
        /// <param name="state">The state.</param>
        private async void TimerCallback(object state)
        {
            var connection = (WebSocketConnection)state;

            Tuple<WebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim> tuple;

            lock (ActiveConnections)
            {
                tuple = ActiveConnections.FirstOrDefault(c => c.Item1 == connection);
            }

            if (tuple == null)
            {
                return;
            }

            if (connection.State != WebSocketState.Open || tuple.Item2.IsCancellationRequested)
            {
                DisposeConnection(tuple);
                return;
            }

            try
            {
                await tuple.Item5.WaitAsync(tuple.Item2.Token).ConfigureAwait(false);

                var data = await GetDataToSend(tuple.Item4).ConfigureAwait(false);

                await connection.SendAsync(new WebSocketMessage<TReturnDataType>
                {
                    MessageType = Name,
                    Data = data

                }, tuple.Item2.Token).ConfigureAwait(false);
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
                Logger.LogException("Error sending web socket message {0}", ex, Name);
                DisposeConnection(tuple);
            }
            finally
            {
                tuple.Item5.Release();
            }
        }

        /// <summary>
        /// Stops sending messages over a web socket
        /// </summary>
        /// <param name="message">The message.</param>
        private void Stop(WebSocketMessageInfo message)
        {
            lock (ActiveConnections)
            {
                var connection = ActiveConnections.FirstOrDefault(c => c.Item1 == message.Connection);

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
        private void DisposeConnection(Tuple<WebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim> connection)
        {
            Logger.LogInfo("{1} stop transmitting over websocket to {0}", connection.Item1.RemoteEndPoint, GetType().Name);

            try
            {
                connection.Item3.Dispose();
            }
            catch (ObjectDisposedException)
            {

            }

            try
            {
                connection.Item2.Cancel();
                connection.Item2.Dispose();
            }
            catch (ObjectDisposedException)
            {
                
            }

            try
            {
                connection.Item5.Dispose();
            }
            catch (ObjectDisposedException)
            {

            }
            
            ActiveConnections.Remove(connection);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                lock (ActiveConnections)
                {
                    foreach (var connection in ActiveConnections.ToList())
                    {
                        DisposeConnection(connection);
                    }
                }
            }

            base.Dispose(dispose);
        }
    }
}
