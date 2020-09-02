#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        private readonly string _defaultRedirectPath;
        private readonly string _baseUrlPrefix;

        private IWebSocketListener[] _webSocketListeners = Array.Empty<IWebSocketListener>();
        private bool _disposed = false;

        public HttpListenerHost(
            ILogger<HttpListenerHost> logger,
            IServerConfigurationManager config,
            IConfiguration configuration,
            INetworkManager networkManager,
            ILocalizationManager localizationManager,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _config = config;
            _defaultRedirectPath = configuration[DefaultRedirectKey];
            _baseUrlPrefix = _config.Configuration.BaseUrl;
            _networkManager = networkManager;
            _loggerFactory = loggerFactory;

            Instance = this;
            GlobalResponse = localizationManager.GetLocalizedString("StartupEmbyServerIsLoading");
        }

        public event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;

        public static HttpListenerHost Instance { get; protected set; }

        public string[] UrlPrefixes { get; private set; }

        public string GlobalResponse { get; set; }

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

        /// <inheritdoc />
        public Task RequestHandler(HttpContext context, Func<Task> next)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                return WebSocketRequestHandler(context);
            }

            return HttpRequestHandler(context, next);
        }

        /// <summary>
        /// Overridable method that can be used to implement a custom handler.
        /// </summary>
        private async Task HttpRequestHandler(HttpContext httpContext, Func<Task> next)
        {
            var cancellationToken = httpContext.RequestAborted;
            var httpRes = httpContext.Response;
            var host = httpContext.Request.Host.ToString();
            var localPath = httpContext.Request.Path.ToString();
            string remoteIp = httpContext.Request.RemoteIp();

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

            await next().ConfigureAwait(false);
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

        /// <inheritdoc />
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
