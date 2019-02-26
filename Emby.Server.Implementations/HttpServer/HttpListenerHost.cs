using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Net;
using Emby.Server.Implementations.Services;
using Emby.Server.Implementations.SocketSharp;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceStack.Text.Jsv;

namespace Emby.Server.Implementations.HttpServer
{
    public class HttpListenerHost : IHttpServer, IDisposable
    {
        private string DefaultRedirectPath { get; set; }

        private readonly ILogger _logger;
        public string[] UrlPrefixes { get; private set; }

        private IHttpListener _listener;

        public event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;

        private readonly IServerConfigurationManager _config;
        private readonly INetworkManager _networkManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IHttpListener _socketListener;
        private readonly Func<Type, Func<string, object>> _funcParseFn;

        public Action<IRequest, IResponse, object>[] ResponseFilters { get; set; }

        private readonly Dictionary<Type, Type> ServiceOperationsMap = new Dictionary<Type, Type>();
        public static HttpListenerHost Instance { get; protected set; }

        private IWebSocketListener[] _webSocketListeners = Array.Empty<IWebSocketListener>();
        private readonly List<IWebSocketConnection> _webSocketConnections = new List<IWebSocketConnection>();

        public HttpListenerHost(
            IServerApplicationHost applicationHost,
            ILoggerFactory loggerFactory,
            IServerConfigurationManager config,
            IConfiguration configuration,
            INetworkManager networkManager,
            IJsonSerializer jsonSerializer,
            IXmlSerializer xmlSerializer,
            IHttpListener socketListener)
        {
            _appHost = applicationHost;
            _logger = loggerFactory.CreateLogger("HttpServer");
            _config = config;
            DefaultRedirectPath = configuration["HttpListenerHost:DefaultRedirectPath"];
            _networkManager = networkManager;
            _jsonSerializer = jsonSerializer;
            _xmlSerializer = xmlSerializer;
            _socketListener = socketListener;
            _socketListener.WebSocketConnected = OnWebSocketConnected;

            _funcParseFn = t => s => JsvReader.GetParseFn(t)(s);

            Instance = this;
            ResponseFilters = Array.Empty<Action<IRequest, IResponse, object>>();
        }

        public string GlobalResponse { get; set; }

        protected ILogger Logger => _logger;

        public object CreateInstance(Type type)
        {
            return _appHost.CreateInstance(type);
        }

        /// <summary>
        /// Applies the request filters. Returns whether or not the request has been handled
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public void ApplyRequestFilters(IRequest req, IResponse res, object requestDto)
        {
            //Exec all RequestFilter attributes with Priority < 0
            var attributes = GetRequestFilterAttributes(requestDto.GetType());

            int count = attributes.Count;
            int i = 0;
            for (; i < count && attributes[i].Priority < 0; i++)
            {
                var attribute = attributes[i];
                attribute.RequestFilter(req, res, requestDto);
            }

            //Exec remaining RequestFilter attributes with Priority >= 0
            for (; i < count && attributes[i].Priority >= 0; i++)
            {
                var attribute = attributes[i];
                attribute.RequestFilter(req, res, requestDto);
            }
        }

        public Type GetServiceTypeByRequest(Type requestType)
        {
            ServiceOperationsMap.TryGetValue(requestType, out var serviceType);
            return serviceType;
        }

        public void AddServiceInfo(Type serviceType, Type requestType)
        {
            ServiceOperationsMap[requestType] = serviceType;
        }

        private List<IHasRequestFilter> GetRequestFilterAttributes(Type requestDtoType)
        {
            var attributes = requestDtoType.GetTypeInfo().GetCustomAttributes(true).OfType<IHasRequestFilter>().ToList();

            var serviceType = GetServiceTypeByRequest(requestDtoType);
            if (serviceType != null)
            {
                attributes.AddRange(serviceType.GetTypeInfo().GetCustomAttributes(true).OfType<IHasRequestFilter>());
            }

            attributes.Sort((x, y) => x.Priority - y.Priority);

            return attributes;
        }

        private void OnWebSocketConnected(WebSocketConnectEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            var connection = new WebSocketConnection(e.WebSocket, e.Endpoint, _jsonSerializer, _logger)
            {
                OnReceive = ProcessWebSocketMessageReceived,
                Url = e.Url,
                QueryString = e.QueryString ?? new QueryParamCollection()
            };

            connection.Closed += Connection_Closed;

            lock (_webSocketConnections)
            {
                _webSocketConnections.Add(connection);
            }

            WebSocketConnected?.Invoke(this, new GenericEventArgs<IWebSocketConnection>(connection));
        }

        private void Connection_Closed(object sender, EventArgs e)
        {
            lock (_webSocketConnections)
            {
                _webSocketConnections.Remove((IWebSocketConnection)sender);
            }
        }

        private static Exception GetActualException(Exception ex)
        {
            if (ex is AggregateException agg)
            {
                var inner = agg.InnerException;
                if (inner != null)
                {
                    return GetActualException(inner);
                }
                else
                {
                    var inners = agg.InnerExceptions;
                    if (inners != null && inners.Count > 0)
                    {
                        return GetActualException(inners[0]);
                    }
                }
            }

            return ex;
        }

        private int GetStatusCode(Exception ex)
        {
            switch (ex)
            {
                case ArgumentException _: return 400;
                case SecurityException _: return 401;
                case DirectoryNotFoundException _:
                case FileNotFoundException _:
                case ResourceNotFoundException _: return 404;
                case RemoteServiceUnavailableException _: return 502;
                default: return 500;
            }
        }

        private async Task ErrorHandler(Exception ex, IRequest httpReq, bool logExceptionStackTrace, bool logExceptionMessage)
        {
            try
            {
                ex = GetActualException(ex);

                if (logExceptionStackTrace)
                {
                    _logger.LogError(ex, "Error processing request");
                }
                else if (logExceptionMessage)
                {
                    _logger.LogError(ex.Message);
                }

                var httpRes = httpReq.Response;

                if (httpRes.IsClosed)
                {
                    return;
                }

                var statusCode = GetStatusCode(ex);
                httpRes.StatusCode = statusCode;

                httpRes.ContentType = "text/html";
                await Write(httpRes, NormalizeExceptionMessage(ex.Message)).ConfigureAwait(false);
            }
            catch (Exception errorEx)
            {
                _logger.LogError(errorEx, "Error this.ProcessRequest(context)(Exception while writing error to the response)");
            }
        }

        private string NormalizeExceptionMessage(string msg)
        {
            if (msg == null)
            {
                return string.Empty;
            }

            // Strip any information we don't want to reveal

            msg = msg.Replace(_config.ApplicationPaths.ProgramSystemPath, string.Empty, StringComparison.OrdinalIgnoreCase);
            msg = msg.Replace(_config.ApplicationPaths.ProgramDataPath, string.Empty, StringComparison.OrdinalIgnoreCase);

            return msg;
        }

        /// <summary>
        /// Shut down the Web Service
        /// </summary>
        public void Stop()
        {
            List<IWebSocketConnection> connections;

            lock (_webSocketConnections)
            {
                connections = _webSocketConnections.ToList();
                _webSocketConnections.Clear();
            }

            foreach (var connection in connections)
            {
                try
                {
                    connection.Dispose();
                }
                catch
                {

                }
            }

            if (_listener != null)
            {
                _logger.LogInformation("Stopping HttpListener...");
                var task = _listener.Stop();
                Task.WaitAll(task);
                _logger.LogInformation("HttpListener stopped");
            }
        }

        public static string RemoveQueryStringByKey(string url, string key)
        {
            var uri = new Uri(url);

            // this gets all the query string key value pairs as a collection
            var newQueryString = MyHttpUtility.ParseQueryString(uri.Query);

            var originalCount = newQueryString.Count;

            if (originalCount == 0)
            {
                return url;
            }

            // this removes the key if exists
            newQueryString.Remove(key);

            if (originalCount == newQueryString.Count)
            {
                return url;
            }

            // this gets the page path from root without QueryString
            string pagePathWithoutQueryString = url.Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries)[0];

            return newQueryString.Count > 0
                ? string.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
                : pagePathWithoutQueryString;
        }

        private static string GetUrlToLog(string url)
        {
            url = RemoveQueryStringByKey(url, "api_key");

            return url;
        }

        private static string NormalizeConfiguredLocalAddress(string address)
        {
            var index = address.Trim('/').IndexOf('/');

            if (index != -1)
            {
                address = address.Substring(index + 1);
            }

            return address.Trim('/');
        }

        private bool ValidateHost(string host)
        {
            var hosts = _config
                .Configuration
                .LocalNetworkAddresses
                .Select(NormalizeConfiguredLocalAddress)
                .ToList();

            if (hosts.Count == 0)
            {
                return true;
            }

            host = host ?? string.Empty;

            if (_networkManager.IsInPrivateAddressSpace(host))
            {
                hosts.Add("localhost");
                hosts.Add("127.0.0.1");

                return hosts.Any(i => host.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1);
            }

            return true;
        }

        private bool ValidateRequest(string remoteIp, bool isLocal)
        {
            if (isLocal)
            {
                return true;
            }

            if (_config.Configuration.EnableRemoteAccess)
            {
                var addressFilter = _config.Configuration.RemoteIPFilter.Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();

                if (addressFilter.Length > 0 && !_networkManager.IsInLocalNetwork(remoteIp))
                {
                    if (_config.Configuration.IsRemoteIPFilterBlacklist)
                    {
                        return !_networkManager.IsAddressInSubnets(remoteIp, addressFilter);
                    }
                    else
                    {
                        return _networkManager.IsAddressInSubnets(remoteIp, addressFilter);
                    }
                }
            }
            else
            {
                if (!_networkManager.IsInLocalNetwork(remoteIp))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateSsl(string remoteIp, string urlString)
        {
            if (_config.Configuration.RequireHttps && _appHost.EnableHttps && !_config.Configuration.IsBehindProxy)
            {
                if (urlString.IndexOf("https://", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    // These are hacks, but if these ever occur on ipv6 in the local network they could be incorrectly redirected
                    if (urlString.IndexOf("system/ping", StringComparison.OrdinalIgnoreCase) != -1 ||
                        urlString.IndexOf("dlna/", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return true;
                    }

                    if (!_networkManager.IsInLocalNetwork(remoteIp))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Overridable method that can be used to implement a custom hnandler
        /// </summary>
        public async Task RequestHandler(IHttpRequest httpReq, string urlString, string host, string localPath, CancellationToken cancellationToken)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var httpRes = httpReq.Response;
            string urlToLog = null;
            string remoteIp = httpReq.RemoteIp;

            try
            {
                if (_disposed)
                {
                    httpRes.StatusCode = 503;
                    httpRes.ContentType = "text/plain";
                    await Write(httpRes, "Server shutting down").ConfigureAwait(false);
                    return;
                }

                if (!ValidateHost(host))
                {
                    httpRes.StatusCode = 400;
                    httpRes.ContentType = "text/plain";
                    await Write(httpRes, "Invalid host").ConfigureAwait(false);
                    return;
                }

                if (!ValidateRequest(remoteIp, httpReq.IsLocal))
                {
                    httpRes.StatusCode = 403;
                    httpRes.ContentType = "text/plain";
                    await Write(httpRes, "Forbidden").ConfigureAwait(false);
                    return;
                }

                if (!ValidateSsl(httpReq.RemoteIp, urlString))
                {
                    RedirectToSecureUrl(httpReq, httpRes, urlString);
                    return;
                }

                if (string.Equals(httpReq.Verb, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    httpRes.StatusCode = 200;
                    httpRes.AddHeader("Access-Control-Allow-Origin", "*");
                    httpRes.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
                    httpRes.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, Range, X-MediaBrowser-Token, X-Emby-Authorization");
                    httpRes.ContentType = "text/plain";
                    await Write(httpRes, string.Empty).ConfigureAwait(false);
                    return;
                }

                urlToLog = GetUrlToLog(urlString);
                Logger.LogDebug("HTTP {HttpMethod} {Url} UserAgent: {UserAgent} \nHeaders: {@Headers}", urlToLog, httpReq.UserAgent ?? string.Empty, httpReq.HttpMethod, httpReq.Headers);

                if (string.Equals(localPath, "/emby/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(localPath, "/mediabrowser/", StringComparison.OrdinalIgnoreCase))
                {
                    RedirectToUrl(httpRes, DefaultRedirectPath);
                    return;
                }

                if (string.Equals(localPath, "/emby", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(localPath, "/mediabrowser", StringComparison.OrdinalIgnoreCase))
                {
                    RedirectToUrl(httpRes, "emby/" + DefaultRedirectPath);
                    return;
                }

                if (localPath.IndexOf("mediabrowser/web", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    httpRes.StatusCode = 200;
                    httpRes.ContentType = "text/html";
                    var newUrl = urlString.Replace("mediabrowser", "emby", StringComparison.OrdinalIgnoreCase)
                        .Replace("/dashboard/", "/web/", StringComparison.OrdinalIgnoreCase);

                    if (!string.Equals(newUrl, urlString, StringComparison.OrdinalIgnoreCase))
                    {
                        await Write(httpRes,
                            "<!doctype html><html><head><title>Emby</title></head><body>Please update your Emby bookmark to <a href=\"" +
                            newUrl + "\">" + newUrl + "</a></body></html>").ConfigureAwait(false);
                        return;
                    }
                }

                if (localPath.IndexOf("dashboard/", StringComparison.OrdinalIgnoreCase) != -1 &&
                    localPath.IndexOf("web/dashboard", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    httpRes.StatusCode = 200;
                    httpRes.ContentType = "text/html";
                    var newUrl = urlString.Replace("mediabrowser", "emby", StringComparison.OrdinalIgnoreCase)
                        .Replace("/dashboard/", "/web/", StringComparison.OrdinalIgnoreCase);

                    if (!string.Equals(newUrl, urlString, StringComparison.OrdinalIgnoreCase))
                    {
                        await Write(httpRes,
                            "<!doctype html><html><head><title>Emby</title></head><body>Please update your Emby bookmark to <a href=\"" +
                            newUrl + "\">" + newUrl + "</a></body></html>").ConfigureAwait(false);
                        return;
                    }
                }

                if (string.Equals(localPath, "/web", StringComparison.OrdinalIgnoreCase))
                {
                    RedirectToUrl(httpRes, DefaultRedirectPath);
                    return;
                }

                if (string.Equals(localPath, "/web/", StringComparison.OrdinalIgnoreCase))
                {
                    RedirectToUrl(httpRes, "../" + DefaultRedirectPath);
                    return;
                }

                if (string.Equals(localPath, "/", StringComparison.OrdinalIgnoreCase))
                {
                    RedirectToUrl(httpRes, DefaultRedirectPath);
                    return;
                }

                if (string.IsNullOrEmpty(localPath))
                {
                    RedirectToUrl(httpRes, "/" + DefaultRedirectPath);
                    return;
                }

                if (!string.Equals(httpReq.QueryString["r"], "0", StringComparison.OrdinalIgnoreCase))
                {
                    if (localPath.EndsWith("web/dashboard.html", StringComparison.OrdinalIgnoreCase))
                    {
                        RedirectToUrl(httpRes, "index.html#!/dashboard.html");
                    }

                    if (localPath.EndsWith("web/home.html", StringComparison.OrdinalIgnoreCase))
                    {
                        RedirectToUrl(httpRes, "index.html");
                    }
                }

                if (!string.IsNullOrEmpty(GlobalResponse))
                {
                    // We don't want the address pings in ApplicationHost to fail
                    if (localPath.IndexOf("system/ping", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        httpRes.StatusCode = 503;
                        httpRes.ContentType = "text/html";
                        await Write(httpRes, GlobalResponse).ConfigureAwait(false);
                        return;
                    }
                }

                var handler = GetServiceHandler(httpReq);

                if (handler != null)
                {
                    await handler.ProcessRequestAsync(this, httpReq, httpRes, Logger, httpReq.OperationName, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ErrorHandler(new FileNotFoundException(), httpReq, false, false).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException || ex is OperationCanceledException)
            {
                await ErrorHandler(ex, httpReq, false, false).ConfigureAwait(false);
            }
            catch (SecurityException ex)
            {
                await ErrorHandler(ex, httpReq, false, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var logException = !string.Equals(ex.GetType().Name, "SocketException", StringComparison.OrdinalIgnoreCase);

                await ErrorHandler(ex, httpReq, logException, false).ConfigureAwait(false);
            }
            finally
            {
                httpRes.Close();

                stopWatch.Stop();
                var elapsed = stopWatch.Elapsed;
                if (elapsed.TotalMilliseconds > 500)
                {
                    _logger.LogWarning("HTTP Response {StatusCode} to {RemoteIp}. Time (slow): {Elapsed:g}. {Url}", httpRes.StatusCode, remoteIp, elapsed, urlToLog);
                }
                else
                {
                    _logger.LogDebug("HTTP Response {StatusCode} to {RemoteIp}. Time: {Elapsed:g}. {Url}", httpRes.StatusCode, remoteIp, elapsed, urlToLog);
                }
            }
        }

        // Entry point for HttpListener
        public ServiceHandler GetServiceHandler(IHttpRequest httpReq)
        {
            var pathInfo = httpReq.PathInfo;

            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0)
            {
                _logger.LogError("Path parts empty for PathInfo: {PathInfo}, Url: {RawUrl}", pathInfo, httpReq.RawUrl);
                return null;
            }

            var restPath = ServiceHandler.FindMatchingRestPath(httpReq.HttpMethod, pathInfo, out string contentType);
            if (restPath != null)
            {
                return new ServiceHandler
                {
                    RestPath = restPath,
                    ResponseContentType = contentType
                };
            }

            _logger.LogError("Could not find handler for {PathInfo}", pathInfo);
            return null;
        }

        private static Task Write(IResponse response, string text)
        {
            var bOutput = Encoding.UTF8.GetBytes(text);
            return response.OutputStream.WriteAsync(bOutput, 0, bOutput.Length);
        }

        private void RedirectToSecureUrl(IHttpRequest httpReq, IResponse httpRes, string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                var builder = new UriBuilder(uri)
                {
                    Port = _config.Configuration.PublicHttpsPort,
                    Scheme = "https"
                };
                url = builder.Uri.ToString();

                RedirectToUrl(httpRes, url);
            }
            else
            {
                // TODO what is this?
                var httpsUrl = url
                    .Replace("http://", "https://", StringComparison.OrdinalIgnoreCase)
                    .Replace(":" + _config.Configuration.PublicPort.ToString(CultureInfo.InvariantCulture), ":" + _config.Configuration.PublicHttpsPort.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

                RedirectToUrl(httpRes, url);
            }
        }

        public static void RedirectToUrl(IResponse httpRes, string url)
        {
            httpRes.StatusCode = 302;
            httpRes.AddHeader("Location", url);
        }

        public ServiceController ServiceController { get; private set; }

        /// <summary>
        /// Adds the rest handlers.
        /// </summary>
        /// <param name="services">The services.</param>
        public void Init(IEnumerable<IService> services, IEnumerable<IWebSocketListener> listeners)
        {
            _webSocketListeners = listeners.ToArray();

            ServiceController = new ServiceController();

            _logger.LogInformation("Calling ServiceStack AppHost.Init");

            var types = services.Select(r => r.GetType()).ToArray();

            ServiceController.Init(this, types);

            ResponseFilters = new Action<IRequest, IResponse, object>[]
            {
                new ResponseFilter(_logger).FilterResponse
            };
        }

        public RouteAttribute[] GetRouteAttributes(Type requestType)
        {
            var routes = requestType.GetTypeInfo().GetCustomAttributes<RouteAttribute>(true).ToList();
            var clone = routes.ToList();

            foreach (var route in clone)
            {
                routes.Add(new RouteAttribute(NormalizeEmbyRoutePath(route.Path), route.Verbs)
                {
                    Notes = route.Notes,
                    Priority = route.Priority,
                    Summary = route.Summary
                });

                routes.Add(new RouteAttribute(NormalizeMediaBrowserRoutePath(route.Path), route.Verbs)
                {
                    Notes = route.Notes,
                    Priority = route.Priority,
                    Summary = route.Summary
                });

                // needed because apps add /emby, and some users also add /emby, thereby double prefixing
                routes.Add(new RouteAttribute(DoubleNormalizeEmbyRoutePath(route.Path), route.Verbs)
                {
                    Notes = route.Notes,
                    Priority = route.Priority,
                    Summary = route.Summary
                });
            }

            return routes.ToArray();
        }

        public Func<string, object> GetParseFn(Type propertyType)
        {
            return _funcParseFn(propertyType);
        }

        public void SerializeToJson(object o, Stream stream)
        {
            _jsonSerializer.SerializeToStream(o, stream);
        }

        public void SerializeToXml(object o, Stream stream)
        {
            _xmlSerializer.SerializeToStream(o, stream);
        }

        public Task<object> DeserializeXml(Type type, Stream stream)
        {
            return Task.FromResult(_xmlSerializer.DeserializeFromStream(type, stream));
        }

        public Task<object> DeserializeJson(Type type, Stream stream)
        {
            return _jsonSerializer.DeserializeFromStreamAsync(stream, type);
        }

        public Task ProcessWebSocketRequest(HttpContext context)
        {
            // TODO
            return ((WebSocketSharpListener)_socketListener).ProcessWebSocketRequest(context);
        }
        //TODO Add Jellyfin Route Path Normalizer

        private static string NormalizeEmbyRoutePath(string path)
        {
            if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return "/emby" + path;
            }

            return "emby/" + path;
        }

        private static string NormalizeMediaBrowserRoutePath(string path)
        {
            if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return "/mediabrowser" + path;
            }

            return "mediabrowser/" + path;
        }

        private static string DoubleNormalizeEmbyRoutePath(string path)
        {
            if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return "/emby/emby" + path;
            }

            return "emby/emby/" + path;
        }

        private bool _disposed;
        private readonly object _disposeLock = new object();
        private readonly WebSocketSharpListener _websocketlistener;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            lock (_disposeLock)
            {
                if (_disposed) return;

                _disposed = true;

                if (disposing)
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// Processes the web socket message received.
        /// </summary>
        /// <param name="result">The result.</param>
        private Task ProcessWebSocketMessageReceived(WebSocketMessageInfo result)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            _logger.LogDebug("Websocket message received: {0}", result.MessageType);

            var tasks = _webSocketListeners.Select(i => Task.Run(async () =>
            {
                try
                {
                    await i.ProcessMessage(result).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{0} failed processing WebSocket message {1}", i.GetType().Name, result.MessageType ?? string.Empty);
                }
            }));

            return Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void StartServer(string[] urlPrefixes, IHttpListener httpListener)
        {
            UrlPrefixes = urlPrefixes;

            _listener = httpListener;

            _listener.WebSocketConnected = OnWebSocketConnected;
            _listener.ErrorHandler = ErrorHandler;
            _listener.RequestHandler = RequestHandler;
        }
    }
}
