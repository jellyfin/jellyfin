using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Starts sending data over a web socket periodically when a message is received, and then stops when a corresponding stop message is received
    /// </summary>
    /// <typeparam name="TReturnDataType">The type of the T return data type.</typeparam>
    /// <typeparam name="TStateType">The type of the T state type.</typeparam>
    public abstract class BasePeriodicWebSocketListener<TReturnDataType, TStateType> : IWebSocketListener, IDisposable
        where TStateType : class, new()
        where TReturnDataType : class
    {
        /// <summary>
        /// The _active connections
        /// </summary>
        protected readonly List<Tuple<IWebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim>> ActiveConnections =
            new List<Tuple<IWebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim>>();

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
        /// The logger
        /// </summary>
        protected ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePeriodicWebSocketListener{TStateType}" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">logger</exception>
        protected BasePeriodicWebSocketListener(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            Logger = logger;
        }

        /// <summary>
        /// The null task result
        /// </summary>
        protected Task NullTaskResult = Task.FromResult(true);

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task ProcessMessage(WebSocketMessageInfo message)
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

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        /// <summary>
        /// Starts sending messages over a web socket
        /// </summary>
        /// <param name="message">The message.</param>
        private void Start(WebSocketMessageInfo message)
        {
            var vals = message.Data.Split(',');

            var dueTimeMs = long.Parse(vals[0], UsCulture);
            var periodMs = long.Parse(vals[1], UsCulture);

            var cancellationTokenSource = new CancellationTokenSource();

            Logger.Info("{1} Begin transmitting over websocket to {0}", message.Connection.RemoteEndPoint, GetType().Name);

            var timer = new Timer(TimerCallback, message.Connection, Timeout.Infinite, Timeout.Infinite);

            var state = new TStateType();

            var semaphore = new SemaphoreSlim(1, 1);

            lock (ActiveConnections)
            {
                ActiveConnections.Add(new Tuple<IWebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim>(message.Connection, cancellationTokenSource, timer, state, semaphore));
            }

            timer.Change(TimeSpan.FromMilliseconds(dueTimeMs), TimeSpan.FromMilliseconds(periodMs));
        }

        /// <summary>
        /// Timers the callback.
        /// </summary>
        /// <param name="state">The state.</param>
        private async void TimerCallback(object state)
        {
            var connection = (IWebSocketConnection)state;

            Tuple<IWebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim> tuple;

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

                if (data != null)
                {
                    await connection.SendAsync(new WebSocketMessage<TReturnDataType>
                    {
                        MessageType = Name,
                        Data = data

                    }, tuple.Item2.Token).ConfigureAwait(false);
                }

                tuple.Item5.Release();
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
                Logger.ErrorException("Error sending web socket message {0}", ex, Name);
                DisposeConnection(tuple);
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
        private void DisposeConnection(Tuple<IWebSocketConnection, CancellationTokenSource, Timer, TStateType, SemaphoreSlim> connection)
        {
            Logger.Info("{1} stop transmitting over websocket to {0}", connection.Item1.RemoteEndPoint, GetType().Name);

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
        protected virtual void Dispose(bool dispose)
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
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
