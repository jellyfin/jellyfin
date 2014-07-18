using Amib.Threading;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using ServiceStack;
using ServiceStack.Host.HttpListener;
using ServiceStack.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.HttpServer.NetListener
{
    public class HttpListenerServer : IHttpListener
    {
        private readonly ILogger _logger;
        private HttpListener _listener;
        private readonly AutoResetEvent _listenForNextRequest = new AutoResetEvent(false);
        private readonly SmartThreadPool _threadPoolManager;

        public System.Action<Exception, IRequest> ErrorHandler { get; set; }
        public Action<WebSocketConnectEventArgs> WebSocketHandler { get; set; }
        public System.Func<IHttpRequest, Uri, Task> RequestHandler { get; set; }

        private readonly ConcurrentDictionary<string, string> _localEndPoints = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        public HttpListenerServer(ILogger logger, SmartThreadPool threadPoolManager)
        {
            _logger = logger;

            _threadPoolManager = threadPoolManager;
        }

        /// <summary>
        /// Gets the local end points.
        /// </summary>
        /// <value>The local end points.</value>
        public IEnumerable<string> LocalEndPoints
        {
            get { return _localEndPoints.Keys.ToList(); }
        }
        
        private List<string> UrlPrefixes { get; set; }

        public void Start(IEnumerable<string> urlPrefixes)
        {
            UrlPrefixes = urlPrefixes.ToList();

            if (_listener == null)
                _listener = new System.Net.HttpListener();

            //HostContext.Config.HandlerFactoryPath = ListenerRequest.GetHandlerPathIfAny(UrlPrefixes.First());

            foreach (var prefix in UrlPrefixes)
            {
                _logger.Info("Adding HttpListener prefix " + prefix);
                _listener.Prefixes.Add(prefix);
            }

            _listener.Start();

            ThreadPool.QueueUserWorkItem(Listen);
        }

        private bool IsListening
        {
            get { return _listener != null && _listener.IsListening; }
        }

        // Loop here to begin processing of new requests.
        private void Listen(object state)
        {
            while (IsListening)
            {
                if (_listener == null) return;

                try
                {
                    _listener.BeginGetContext(ListenerCallback, _listener);
                    _listenForNextRequest.WaitOne();
                }
                catch (Exception ex)
                {
                    _logger.Error("Listen()", ex);
                    return;
                }
                if (_listener == null) return;
            }
        }

        // Handle the processing of a request in here.
        private void ListenerCallback(IAsyncResult asyncResult)
        {
            var listener = asyncResult.AsyncState as HttpListener;
            HttpListenerContext context;

            if (listener == null) return;
            var isListening = listener.IsListening;

            try
            {
                if (!isListening)
                {
                    _logger.Debug("Ignoring ListenerCallback() as HttpListener is no longer listening"); return;
                }
                // The EndGetContext() method, as with all Begin/End asynchronous methods in the .NET Framework,
                // blocks until there is a request to be processed or some type of data is available.
                context = listener.EndGetContext(asyncResult);
            }
            catch (Exception ex)
            {
                // You will get an exception when httpListener.Stop() is called
                // because there will be a thread stopped waiting on the .EndGetContext()
                // method, and again, that is just the way most Begin/End asynchronous
                // methods of the .NET Framework work.
                var errMsg = ex + ": " + IsListening;
                _logger.Warn(errMsg);
                return;
            }
            finally
            {
                // Once we know we have a request (or exception), we signal the other thread
                // so that it calls the BeginGetContext() (or possibly exits if we're not
                // listening any more) method to start handling the next incoming request
                // while we continue to process this request on a different thread.
                _listenForNextRequest.Set();
            }

            _threadPoolManager.QueueWorkItem(() => InitTask(context));
        }

        public virtual void InitTask(HttpListenerContext context)
        {
            try
            {
                var task = this.ProcessRequestAsync(context);
                task.ContinueWith(x => HandleError(x.Exception, context), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.AttachedToParent);

                if (task.Status == TaskStatus.Created)
                {
                    task.RunSynchronously();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, context);
            }
        }

        protected Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;

            LogHttpRequest(request);

            if (request.IsWebSocketRequest)
            {
                return ProcessWebSocketRequest(context);
            }

            if (string.IsNullOrEmpty(context.Request.RawUrl))
                return ((object)null).AsTaskResult();

            var operationName = context.Request.GetOperationName();

            var httpReq = GetRequest(context, operationName);

            return RequestHandler(httpReq, request.Url);
        }

        /// <summary>
        /// Processes the web socket request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        private async Task ProcessWebSocketRequest(HttpListenerContext ctx)
        {
#if !__MonoCS__
            try
            {
                var webSocketContext = await ctx.AcceptWebSocketAsync(null).ConfigureAwait(false);

                if (WebSocketHandler != null)
                {
                    WebSocketHandler(new WebSocketConnectEventArgs
                    {
                        WebSocket = new NativeWebSocket(webSocketContext.WebSocket, _logger),
                        Endpoint = ctx.Request.RemoteEndPoint.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("AcceptWebSocketAsync error", ex);
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
#endif
        }

        private void HandleError(Exception ex, HttpListenerContext context)
        {
            var operationName = context.Request.GetOperationName();
            var httpReq = GetRequest(context, operationName);
            
            if (ErrorHandler != null)
            {
                ErrorHandler(ex, httpReq);
            }
        }

        private static ListenerRequest GetRequest(HttpListenerContext httpContext, string operationName)
        {
            var req = new ListenerRequest(httpContext, operationName, RequestAttributes.None);
            req.RequestAttributes = req.GetAttributes();

            return req;
        }

        /// <summary>
        /// Logs the HTTP request.
        /// </summary>
        /// <param name="request">The request.</param>
        private void LogHttpRequest(HttpListenerRequest request)
        {
            var endpoint = request.LocalEndPoint;

            if (endpoint != null)
            {
                var address = endpoint.ToString();

                _localEndPoints.GetOrAdd(address, address);
            }

            LoggerUtils.LogRequest(_logger, request);
        }

        public void Stop()
        {
            if (_listener != null)
            {
                foreach (var prefix in UrlPrefixes)
                {
                    _listener.Prefixes.Remove(prefix);
                }

                _listener.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private bool _disposed;
        private readonly object _disposeLock = new object();
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            lock (_disposeLock)
            {
                if (_disposed) return;

                if (disposing)
                {
                    _threadPoolManager.Dispose();

                    Stop();
                }

                //release unmanaged resources here...
                _disposed = true;
            }
        }
    }
}
