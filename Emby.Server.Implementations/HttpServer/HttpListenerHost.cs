#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Emby.Server.Implementations.HttpServer
{
    public class HttpListenerHost : IHttpServer
    {
        /// <summary>
        /// The key for a setting that specifies the default redirect path
        /// to use for requests where the URL base prefix is invalid or missing.
        /// </summary>
        public const string DefaultRedirectKey = "HttpListenerHost:DefaultRedirectPath";

        private readonly ILogger<HttpListenerHost> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServerConfigurationManager _config;
        private readonly INetworkManager _networkManager;
        private readonly IServerApplicationHost _appHost;
        private readonly string _defaultRedirectPath;
        private readonly string _baseUrlPrefix;

        private readonly IHostEnvironment _hostEnvironment;

        private IWebSocketListener[] _webSocketListeners = Array.Empty<IWebSocketListener>();
        private bool _disposed = false;

        public HttpListenerHost(
            IServerApplicationHost applicationHost,
            ILogger<HttpListenerHost> logger,
            IServerConfigurationManager config,
            IConfiguration configuration,
            INetworkManager networkManager,
            ILocalizationManager localizationManager,
            IHostEnvironment hostEnvironment,
            ILoggerFactory loggerFactory)
        {
            _appHost = applicationHost;
            _logger = logger;
            _config = config;
            _defaultRedirectPath = configuration[DefaultRedirectKey];
            _baseUrlPrefix = _config.Configuration.BaseUrl;
            _networkManager = networkManager;
            _hostEnvironment = hostEnvironment;
            _loggerFactory = loggerFactory;

            Instance = this;
            GlobalResponse = localizationManager.GetLocalizedString("StartupEmbyServerIsLoading");
        }

        public event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;

        public static HttpListenerHost Instance { get; protected set; }

        public string[] UrlPrefixes { get; private set; }

        public string GlobalResponse { get; set; }

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

        private async Task ErrorHandler(Exception ex, HttpContext httpContext, int statusCode, string urlToLog, bool ignoreStackTrace)
        {
            if (ignoreStackTrace)
            {
                _logger.LogError("Error processing request: {Message}. URL: {Url}", ex.Message.TrimEnd('.'), urlToLog);
            }
            else
            {
                _logger.LogError(ex, "Error processing request. URL: {Url}", urlToLog);
            }

            var httpRes = httpContext.Response;

            if (httpRes.HasStarted)
            {
                return;
            }

            httpRes.StatusCode = statusCode;

            var errContent = _hostEnvironment.IsDevelopment()
                    ? (NormalizeExceptionMessage(ex) ?? string.Empty)
                    : "Error processing request.";
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

            return RequestHandler(context, context.RequestAborted);
        }

        /// <summary>
        /// Overridable method that can be used to implement a custom handler.
        /// </summary>
        private async Task RequestHandler(HttpContext httpContext, CancellationToken cancellationToken)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var httpRes = httpContext.Response;
            var host = httpContext.Request.Host.ToString();
            var localPath = httpContext.Request.Path.ToString();
            var urlString = httpContext.Request.GetDisplayUrl();
            string urlToLog = GetUrlToLog(urlString);
            string remoteIp = httpContext.Request.RemoteIp();

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

                if (!ValidateRequest(remoteIp, httpContext.Request.IsLocal()))
                {
                    httpRes.StatusCode = 403;
                    httpRes.ContentType = "text/plain";
                    await httpRes.WriteAsync("Forbidden", cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (!ValidateSsl(httpContext.Request.RemoteIp(), urlString))
                {
                    RedirectToSecureUrl(httpRes, urlString);
                    return;
                }

                if (string.Equals(httpContext.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    httpRes.StatusCode = 200;
                    foreach (var (key, value) in GetDefaultCorsHeaders(httpContext))
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

                throw new FileNotFoundException();
            }
            catch (Exception requestEx)
            {
                try
                {
                    var requestInnerEx = GetActualException(requestEx);
                    var statusCode = GetStatusCode(requestInnerEx);

                    foreach (var (key, value) in GetDefaultCorsHeaders(httpContext))
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

                    await ErrorHandler(requestInnerEx, httpContext, statusCode, urlToLog, ignoreStackTrace).ConfigureAwait(false);
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

                using var connection = new WebSocketConnection(
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
        /// Get the default CORS headers.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public IDictionary<string, string> GetDefaultCorsHeaders(HttpContext httpContext)
        {
            var origin = httpContext.Request.Headers["Origin"];
            if (origin == StringValues.Empty)
            {
                origin = httpContext.Request.Headers["Host"];
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

        private void RedirectToSecureUrl(HttpResponse httpRes, string url)
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
        /// <param name="listeners">The web socket listeners.</param>
        /// <param name="urlPrefixes">The URL prefixes. See <see cref="UrlPrefixes"/>.</param>
        public void Init(IEnumerable<IWebSocketListener> listeners, IEnumerable<string> urlPrefixes)
        {
            _webSocketListeners = listeners.ToArray();
            UrlPrefixes = urlPrefixes.ToArray();
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
