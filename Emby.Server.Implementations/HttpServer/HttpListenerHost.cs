#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Services;
using Emby.Server.Implementations.SocketSharp;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using ServiceStack.Text.Jsv;

namespace Emby.Server.Implementations.HttpServer
{
    public class HttpListenerHost : IHttpServer
    {
        /// <summary>
        /// The key for a setting that specifies the default redirect path
        /// to use for requests where the URL base prefix is invalid or missing.
        /// </summary>
        public const string DefaultRedirectKey = "HttpListenerHost:DefaultRedirectPath";

        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServerConfigurationManager _config;
        private readonly INetworkManager _networkManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly Func<Type, Func<string, object>> _funcParseFn;
        private readonly string _defaultRedirectPath;
        private readonly string _baseUrlPrefix;

        private readonly Dictionary<Type, Type> _serviceOperationsMap = new Dictionary<Type, Type>();
        private readonly IHostEnvironment _hostEnvironment;

        private IWebSocketListener[] _webSocketListeners = Array.Empty<IWebSocketListener>();
        private bool _disposed = false;

        public HttpListenerHost(
            IServerApplicationHost applicationHost,
            ILogger<HttpListenerHost> logger,
            IServerConfigurationManager config,
            IConfiguration configuration,
            INetworkManager networkManager,
            IJsonSerializer jsonSerializer,
            IXmlSerializer xmlSerializer,
            ILocalizationManager localizationManager,
            ServiceController serviceController,
            IHostEnvironment hostEnvironment,
            ILoggerFactory loggerFactory)
        {
            _appHost = applicationHost;
            _logger = logger;
            _config = config;
            _defaultRedirectPath = configuration[DefaultRedirectKey];
            _baseUrlPrefix = _config.Configuration.BaseUrl;
            _networkManager = networkManager;
            _jsonSerializer = jsonSerializer;
            _xmlSerializer = xmlSerializer;
            ServiceController = serviceController;
            _hostEnvironment = hostEnvironment;
            _loggerFactory = loggerFactory;

            _funcParseFn = t => s => JsvReader.GetParseFn(t)(s);

            Instance = this;
            ResponseFilters = Array.Empty<Action<IRequest, HttpResponse, object>>();
            GlobalResponse = localizationManager.GetLocalizedString("StartupEmbyServerIsLoading");
        }

        public event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;

        public Action<IRequest, HttpResponse, object>[] ResponseFilters { get; set; }

        public static HttpListenerHost Instance { get; protected set; }

        public string[] UrlPrefixes { get; private set; }

        public string GlobalResponse { get; set; }

        public ServiceController ServiceController { get; }

        public object CreateInstance(Type type)
        {
            return _appHost.CreateInstance(type);
        }

        private static string NormalizeUrlPath(string path)
        {
            if (path.Length > 0 && path[0] == '/')
            {
                // If the path begins with a leading slash, just return it as-is
                return path;
            }
            else
            {
                // If the path does not begin with a leading slash, append one for consistency
                return "/" + path;
            }
        }

        /// <summary>
        /// Applies the request filters. Returns whether or not the request has been handled
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public void ApplyRequestFilters(IRequest req, HttpResponse res, object requestDto)
        {
            // Exec all RequestFilter attributes with Priority < 0
            var attributes = GetRequestFilterAttributes(requestDto.GetType());

            int count = attributes.Count;
            int i = 0;
            for (; i < count && attributes[i].Priority < 0; i++)
            {
                var attribute = attributes[i];
                attribute.RequestFilter(req, res, requestDto);
            }

            // Exec remaining RequestFilter attributes with Priority >= 0
            for (; i < count && attributes[i].Priority >= 0; i++)
            {
                var attribute = attributes[i];
                attribute.RequestFilter(req, res, requestDto);
            }
        }

        public Type GetServiceTypeByRequest(Type requestType)
        {
            _serviceOperationsMap.TryGetValue(requestType, out var serviceType);
            return serviceType;
        }

        public void AddServiceInfo(Type serviceType, Type requestType)
        {
            _serviceOperationsMap[requestType] = serviceType;
        }

        private List<IHasRequestFilter> GetRequestFilterAttributes(Type requestDtoType)
        {
            var attributes = requestDtoType.GetCustomAttributes(true).OfType<IHasRequestFilter>().ToList();

            var serviceType = GetServiceTypeByRequest(requestDtoType);
            if (serviceType != null)
            {
                attributes.AddRange(serviceType.GetCustomAttributes(true).OfType<IHasRequestFilter>());
            }

            attributes.Sort((x, y) => x.Priority - y.Priority);

            return attributes;
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
                    if (inners.Count > 0)
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
                case AuthenticationException _: return 401;
                case SecurityException _: return 403;
                case DirectoryNotFoundException _:
                case FileNotFoundException _:
                case ResourceNotFoundException _: return 404;
                case MethodNotAllowedException _: return 405;
                default: return 500;
            }
        }

        private async Task ErrorHandler(Exception ex, IRequest httpReq, int statusCode, string urlToLog, bool ignoreStackTrace)
        {
            if (ignoreStackTrace)
            {
                _logger.LogError("Error processing request: {Message}. URL: {Url}", ex.Message.TrimEnd('.'), urlToLog);
            }
            else
            {
                _logger.LogError(ex, "Error processing request. URL: {Url}", urlToLog);
            }

            var httpRes = httpReq.Response;

            if (httpRes.HasStarted)
            {
                return;
            }

            httpRes.StatusCode = statusCode;

            var errContent = NormalizeExceptionMessage(ex) ?? string.Empty;
            httpRes.ContentType = "text/plain";
            httpRes.ContentLength = errContent.Length;
            await httpRes.WriteAsync(errContent).ConfigureAwait(false);
        }

        private string NormalizeExceptionMessage(Exception ex)
        {
            // Do not expose the exception message for AuthenticationException
            if (ex is AuthenticationException)
            {
                return null;
            }

            // Strip any information we don't want to reveal
            return ex.Message
                ?.Replace(_config.ApplicationPaths.ProgramSystemPath, string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(_config.ApplicationPaths.ProgramDataPath, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public static string RemoveQueryStringByKey(string url, string key)
        {
            var uri = new Uri(url);

            // this gets all the query string key value pairs as a collection
            var newQueryString = QueryHelpers.ParseQuery(uri.Query);

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
                ? QueryHelpers.AddQueryString(pagePathWithoutQueryString, newQueryString.ToDictionary(kv => kv.Key, kv => kv.Value.ToString()))
                : pagePathWithoutQueryString;
        }

        private static string GetUrlToLog(string url)
        {
            url = RemoveQueryStringByKey(url, "api_key");

            return url;
        }

        private static string NormalizeConfiguredLocalAddress(string address)
        {
            var add = address.AsSpan().Trim('/');
            int index = add.IndexOf('/');
            if (index != -1)
            {
                add = add.Slice(index + 1);
            }

            return add.TrimStart('/').ToString();
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

            host ??= string.Empty;

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

        /// <summary>
        /// Validate a connection from a remote IP address to a URL to see if a redirection to HTTPS is required.
        /// </summary>
        /// <returns>True if the request is valid, or false if the request is not valid and an HTTPS redirect is required.</returns>
        private bool ValidateSsl(string remoteIp, string urlString)
        {
            if (_config.Configuration.RequireHttps
                && _appHost.ListenWithHttps
                && !urlString.Contains("https://", StringComparison.OrdinalIgnoreCase))
            {
                // These are hacks, but if these ever occur on ipv6 in the local network they could be incorrectly redirected
                if (urlString.IndexOf("system/ping", StringComparison.OrdinalIgnoreCase) != -1
                    || urlString.IndexOf("dlna/", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return true;
                }

                if (!_networkManager.IsInLocalNetwork(remoteIp))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public Task RequestHandler(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                return WebSocketRequestHandler(context);
            }

            var request = context.Request;
            var response = context.Response;
            var localPath = context.Request.Path.ToString();

            var req = new WebSocketSharpRequest(request, response, request.Path, _logger);
            return RequestHandler(req, request.GetDisplayUrl(), request.Host.ToString(), localPath, context.RequestAborted);
        }

        /// <summary>
        /// Overridable method that can be used to implement a custom handler.
        /// </summary>
        private async Task RequestHandler(IHttpRequest httpReq, string urlString, string host, string localPath, CancellationToken cancellationToken)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var httpRes = httpReq.Response;
            string urlToLog = GetUrlToLog(urlString);
            string remoteIp = httpReq.RemoteIp;

            try
            {
                if (_disposed)
                {
                    httpRes.StatusCode = 503;
                    httpRes.ContentType = "text/plain";
                    await httpRes.WriteAsync("Server shutting down", cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (!ValidateHost(host))
                {
                    httpRes.StatusCode = 400;
                    httpRes.ContentType = "text/plain";
                    await httpRes.WriteAsync("Invalid host", cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (!ValidateRequest(remoteIp, httpReq.IsLocal))
                {
                    httpRes.StatusCode = 403;
                    httpRes.ContentType = "text/plain";
                    await httpRes.WriteAsync("Forbidden", cancellationToken).ConfigureAwait(false);
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
                    foreach(var (key, value) in GetDefaultCorsHeaders(httpReq))
                    {
                        httpRes.Headers.Add(key, value);
                    }
                    httpRes.ContentType = "text/plain";
                    await httpRes.WriteAsync(string.Empty, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(localPath, _baseUrlPrefix + "/", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(localPath, _baseUrlPrefix, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(localPath, "/", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrEmpty(localPath)
                    || !localPath.StartsWith(_baseUrlPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Always redirect back to the default path if the base prefix is invalid or missing
                    _logger.LogDebug("Normalizing a URL at {0}", localPath);
                    httpRes.Redirect(_baseUrlPrefix + "/" + _defaultRedirectPath);
                    return;
                }

                if (!string.IsNullOrEmpty(GlobalResponse))
                {
                    // We don't want the address pings in ApplicationHost to fail
                    if (localPath.IndexOf("system/ping", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        httpRes.StatusCode = 503;
                        httpRes.ContentType = "text/html";
                        await httpRes.WriteAsync(GlobalResponse, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                }

                var handler = GetServiceHandler(httpReq);
                if (handler != null)
                {
                    await handler.ProcessRequestAsync(this, httpReq, httpRes, _logger, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            catch (Exception requestEx)
            {
                try
                {
                    var requestInnerEx = GetActualException(requestEx);
                    var statusCode = GetStatusCode(requestInnerEx);

                    foreach (var (key, value) in GetDefaultCorsHeaders(httpReq))
                    {
                        if (!httpRes.Headers.ContainsKey(key))
                        {
                            httpRes.Headers.Add(key, value);
                        }
                    }

                    bool ignoreStackTrace =
                        requestInnerEx is SocketException
                        || requestInnerEx is IOException
                        || requestInnerEx is OperationCanceledException
                        || requestInnerEx is SecurityException
                        || requestInnerEx is AuthenticationException
                        || requestInnerEx is FileNotFoundException;

                    // Do not handle 500 server exceptions manually when in development mode.
                    // Instead, re-throw the exception so it can be handled by the DeveloperExceptionPageMiddleware.
                    // However, do not use the DeveloperExceptionPageMiddleware when the stack trace should be ignored,
                    // because it will log the stack trace when it handles the exception.
                    if (statusCode == 500 && !ignoreStackTrace && _hostEnvironment.IsDevelopment())
                    {
                        throw;
                    }

                    await ErrorHandler(requestInnerEx, httpReq, statusCode, urlToLog, ignoreStackTrace).ConfigureAwait(false);
                }
                catch (Exception handlerException)
                {
                    var aggregateEx = new AggregateException("Error while handling request exception", requestEx, handlerException);
                    _logger.LogError(aggregateEx, "Error while handling exception in response to {Url}", urlToLog);

                    if (_hostEnvironment.IsDevelopment())
                    {
                        throw aggregateEx;
                    }
                }
            }
            finally
            {
                if (httpRes.StatusCode >= 500)
                {
                    _logger.LogDebug("Sending HTTP Response 500 in response to {Url}", urlToLog);
                }

                stopWatch.Stop();
                var elapsed = stopWatch.Elapsed;
                if (elapsed.TotalMilliseconds > 500)
                {
                    _logger.LogWarning("HTTP Response {StatusCode} to {RemoteIp}. Time (slow): {Elapsed:g}. {Url}", httpRes.StatusCode, remoteIp, elapsed, urlToLog);
                }
            }
        }

        private async Task WebSocketRequestHandler(HttpContext context)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _logger.LogInformation("WS {IP} request", context.Connection.RemoteIpAddress);

                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

                var connection = new WebSocketConnection(
                    _loggerFactory.CreateLogger<WebSocketConnection>(),
                    webSocket,
                    context.Connection.RemoteIpAddress,
                    context.Request.Query)
                {
                    OnReceive = ProcessWebSocketMessageReceived
                };

                WebSocketConnected?.Invoke(this, new GenericEventArgs<IWebSocketConnection>(connection));

                await connection.ProcessAsync().ConfigureAwait(false);
                _logger.LogInformation("WS {IP} closed", context.Connection.RemoteIpAddress);
            }
            catch (Exception ex) // Otherwise ASP.Net will ignore the exception
            {
                _logger.LogError(ex, "WS {IP} WebSocketRequestHandler error", context.Connection.RemoteIpAddress);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 500;
                }
            }
        }

        /// <summary>
        /// Get the default CORS headers
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public IDictionary<string, string> GetDefaultCorsHeaders(IRequest req)
        {
            var origin = req.Headers["Origin"];
            if (origin == StringValues.Empty)
            {
                origin = req.Headers["Host"];
                if (origin == StringValues.Empty)
                {
                    origin = "*";
                }
            }

            var headers = new Dictionary<string, string>();
            headers.Add("Access-Control-Allow-Origin", origin);
            headers.Add("Access-Control-Allow-Credentials", "true");
            headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
            headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, Range, X-MediaBrowser-Token, X-Emby-Authorization, Cookie");
            return headers;
        }

        // Entry point for HttpListener
        public ServiceHandler GetServiceHandler(IHttpRequest httpReq)
        {
            var pathInfo = httpReq.PathInfo;

            pathInfo = ServiceHandler.GetSanitizedPathInfo(pathInfo, out string contentType);
            var restPath = ServiceController.GetRestPathForRequest(httpReq.HttpMethod, pathInfo);
            if (restPath != null)
            {
                return new ServiceHandler(restPath, contentType);
            }

            _logger.LogError("Could not find handler for {PathInfo}", pathInfo);
            return null;
        }

        private void RedirectToSecureUrl(IHttpRequest httpReq, HttpResponse httpRes, string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                var builder = new UriBuilder(uri)
                {
                    Port = _config.Configuration.PublicHttpsPort,
                    Scheme = "https"
                };
                url = builder.Uri.ToString();
            }

            httpRes.Redirect(url);
        }

        /// <summary>
        /// Adds the rest handlers.
        /// </summary>
        /// <param name="serviceTypes">The service types to register with the <see cref="ServiceController"/>.</param>
        /// <param name="listeners">The web socket listeners.</param>
        /// <param name="urlPrefixes">The URL prefixes. See <see cref="UrlPrefixes"/>.</param>
        public void Init(IEnumerable<Type> serviceTypes, IEnumerable<IWebSocketListener> listeners, IEnumerable<string> urlPrefixes)
        {
            _webSocketListeners = listeners.ToArray();
            UrlPrefixes = urlPrefixes.ToArray();

            ServiceController.Init(this, serviceTypes);

            ResponseFilters = new Action<IRequest, HttpResponse, object>[]
            {
                new ResponseFilter(this, _logger).FilterResponse
            };
        }

        public RouteAttribute[] GetRouteAttributes(Type requestType)
        {
            var routes = requestType.GetTypeInfo().GetCustomAttributes<RouteAttribute>(true).ToList();
            var clone = routes.ToList();

            foreach (var route in clone)
            {
                routes.Add(new RouteAttribute(NormalizeCustomRoutePath(route.Path), route.Verbs)
                {
                    Notes = route.Notes,
                    Priority = route.Priority,
                    Summary = route.Summary
                });

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

        private string NormalizeEmbyRoutePath(string path)
        {
            _logger.LogDebug("Normalizing /emby route");
            return _baseUrlPrefix + "/emby" + NormalizeUrlPath(path);
        }

        private string NormalizeMediaBrowserRoutePath(string path)
        {
            _logger.LogDebug("Normalizing /mediabrowser route");
            return _baseUrlPrefix + "/mediabrowser" + NormalizeUrlPath(path);
        }

        private string NormalizeCustomRoutePath(string path)
        {
            _logger.LogDebug("Normalizing custom route {0}", path);
            return _baseUrlPrefix + NormalizeUrlPath(path);
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

            IEnumerable<Task> GetTasks()
            {
                foreach (var x in _webSocketListeners)
                {
                    yield return x.ProcessMessageAsync(result);
                }
            }

            return Task.WhenAll(GetTasks());
        }
    }
}
