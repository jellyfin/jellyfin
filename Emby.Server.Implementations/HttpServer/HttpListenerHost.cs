using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.HttpServer.SocketSharp;
using Emby.Server.Implementations.Services;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Text;
using SocketHttpListener.Net;
using SocketHttpListener.Primitives;

namespace Emby.Server.Implementations.HttpServer
{
    public class HttpListenerHost : IHttpServer, IDisposable
    {
        private string DefaultRedirectPath { get; set; }

        private readonly ILogger _logger;
        public IEnumerable<string> UrlPrefixes { get; private set; }

        private readonly List<IService> _restServices = new List<IService>();

        private IHttpListener _listener;

        public event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;
        public event EventHandler<WebSocketConnectingEventArgs> WebSocketConnecting;

        private readonly IServerConfigurationManager _config;
        private readonly INetworkManager _networkManager;
        private readonly IMemoryStreamFactory _memoryStreamProvider;

        private readonly IServerApplicationHost _appHost;

        private readonly ITextEncoding _textEncoding;
        private readonly ISocketFactory _socketFactory;
        private readonly ICryptoProvider _cryptoProvider;

        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly ICertificate _certificate;
        private readonly IEnvironmentInfo _environment;
        private readonly IStreamFactory _streamFactory;
        private readonly Func<Type, Func<string, object>> _funcParseFn;
        private readonly bool _enableDualModeSockets;

        public List<Action<IRequest, IResponse, object>> RequestFilters { get; set; }
        public List<Action<IRequest, IResponse, object>> ResponseFilters { get; set; }

        private readonly Dictionary<Type, Type> ServiceOperationsMap = new Dictionary<Type, Type>();
        public static HttpListenerHost Instance { get; protected set; }

        public HttpListenerHost(IServerApplicationHost applicationHost,
            ILogger logger,
            IServerConfigurationManager config,
            string serviceName,
            string defaultRedirectPath, INetworkManager networkManager, IMemoryStreamFactory memoryStreamProvider, ITextEncoding textEncoding, ISocketFactory socketFactory, ICryptoProvider cryptoProvider, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, IEnvironmentInfo environment, ICertificate certificate, IStreamFactory streamFactory, Func<Type, Func<string, object>> funcParseFn, bool enableDualModeSockets, IFileSystem fileSystem)
        {
            Instance = this;

            _appHost = applicationHost;
            DefaultRedirectPath = defaultRedirectPath;
            _networkManager = networkManager;
            _memoryStreamProvider = memoryStreamProvider;
            _textEncoding = textEncoding;
            _socketFactory = socketFactory;
            _cryptoProvider = cryptoProvider;
            _jsonSerializer = jsonSerializer;
            _xmlSerializer = xmlSerializer;
            _environment = environment;
            _certificate = certificate;
            _streamFactory = streamFactory;
            _funcParseFn = funcParseFn;
            _enableDualModeSockets = enableDualModeSockets;
            _fileSystem = fileSystem;
            _config = config;

            _logger = logger;

            RequestFilters = new List<Action<IRequest, IResponse, object>>();
            ResponseFilters = new List<Action<IRequest, IResponse, object>>();
        }

        public string GlobalResponse { get; set; }

        readonly Dictionary<Type, int> _mapExceptionToStatusCode = new Dictionary<Type, int>
            {
                {typeof (ResourceNotFoundException), 404},
                {typeof (RemoteServiceUnavailableException), 502},
                {typeof (FileNotFoundException), 404},
                //{typeof (DirectoryNotFoundException), 404},
                {typeof (SecurityException), 401},
                {typeof (PaymentRequiredException), 402},
                {typeof (ArgumentException), 400}
            };

        protected ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public object CreateInstance(Type type)
        {
            return _appHost.CreateInstance(type);
        }

        private ServiceController CreateServiceController()
        {
            var types = _restServices.Select(r => r.GetType()).ToArray();

            return new ServiceController(() => types);
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
            var i = 0;
            for (; i < attributes.Length && attributes[i].Priority < 0; i++)
            {
                var attribute = attributes[i];
                attribute.RequestFilter(req, res, requestDto);
            }

            //Exec global filters
            foreach (var requestFilter in RequestFilters)
            {
                requestFilter(req, res, requestDto);
            }

            //Exec remaining RequestFilter attributes with Priority >= 0
            for (; i < attributes.Length && attributes[i].Priority >= 0; i++)
            {
                var attribute = attributes[i];
                attribute.RequestFilter(req, res, requestDto);
            }
        }

        public Type GetServiceTypeByRequest(Type requestType)
        {
            Type serviceType;
            ServiceOperationsMap.TryGetValue(requestType, out serviceType);
            return serviceType;
        }

        public void AddServiceInfo(Type serviceType, Type requestType, Type responseType)
        {
            ServiceOperationsMap[requestType] = serviceType;
        }

        private IHasRequestFilter[] GetRequestFilterAttributes(Type requestDtoType)
        {
            var attributes = requestDtoType.GetTypeInfo().GetCustomAttributes(true).OfType<IHasRequestFilter>().ToList();

            var serviceType = GetServiceTypeByRequest(requestDtoType);
            if (serviceType != null)
            {
                attributes.AddRange(serviceType.GetTypeInfo().GetCustomAttributes(true).OfType<IHasRequestFilter>());
            }

            attributes.Sort((x, y) => x.Priority - y.Priority);

            return attributes.ToArray();
        }

        /// <summary>
        /// Starts the Web Service
        /// </summary>
        private void StartListener()
        {
            WebSocketSharpRequest.HandlerFactoryPath = GetHandlerPathIfAny(UrlPrefixes.First());

            _listener = GetListener();

            _listener.WebSocketConnected = OnWebSocketConnected;
            _listener.WebSocketConnecting = OnWebSocketConnecting;
            _listener.ErrorHandler = ErrorHandler;
            _listener.RequestHandler = RequestHandler;

            _listener.Start(UrlPrefixes);
        }

        public static string GetHandlerPathIfAny(string listenerUrl)
        {
            if (listenerUrl == null) return null;
            var pos = listenerUrl.IndexOf("://", StringComparison.OrdinalIgnoreCase);
            if (pos == -1) return null;
            var startHostUrl = listenerUrl.Substring(pos + "://".Length);
            var endPos = startHostUrl.IndexOf('/');
            if (endPos == -1) return null;
            var endHostUrl = startHostUrl.Substring(endPos + 1);
            return string.IsNullOrEmpty(endHostUrl) ? null : endHostUrl.TrimEnd('/');
        }

        private IHttpListener GetListener()
        {
            return new WebSocketSharpListener(_logger,
                _certificate,
                _memoryStreamProvider,
                _textEncoding,
                _networkManager,
                _socketFactory,
                _cryptoProvider,
                _streamFactory,
                _enableDualModeSockets,
                GetRequest,
                _fileSystem,
                _environment);
        }

        private IHttpRequest GetRequest(HttpListenerContext httpContext)
        {
            var operationName = httpContext.Request.GetOperationName();

            var req = new WebSocketSharpRequest(httpContext, operationName, _logger, _memoryStreamProvider);

            return req;
        }

        private void OnWebSocketConnecting(WebSocketConnectingEventArgs args)
        {
            if (_disposed)
            {
                return;
            }

            if (WebSocketConnecting != null)
            {
                WebSocketConnecting(this, args);
            }
        }

        private void OnWebSocketConnected(WebSocketConnectEventArgs args)
        {
            if (_disposed)
            {
                return;
            }

            if (WebSocketConnected != null)
            {
                WebSocketConnected(this, args);
            }
        }

        private Exception GetActualException(Exception ex)
        {
            var agg = ex as AggregateException;
            if (agg != null)
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
            if (ex is ArgumentException)
            {
                return 400;
            }

            var exceptionType = ex.GetType();

            int statusCode;
            if (!_mapExceptionToStatusCode.TryGetValue(exceptionType, out statusCode))
            {
                if (ex is DirectoryNotFoundException)
                {
                    statusCode = 404;
                }
                else
                {
                    statusCode = 500;
                }
            }

            return statusCode;
        }

        private void ErrorHandler(Exception ex, IRequest httpReq, bool logException = true)
        {
            try
            {
                ex = GetActualException(ex);

                if (logException)
                {
                    _logger.ErrorException("Error processing request", ex);
                }

                var httpRes = httpReq.Response;

                if (httpRes.IsClosed)
                {
                    return;
                }

                var statusCode = GetStatusCode(ex);
                httpRes.StatusCode = statusCode;

                httpRes.ContentType = "text/html";
                Write(httpRes, ex.Message);
            }
            catch
            {
                //_logger.ErrorException("Error this.ProcessRequest(context)(Exception while writing error to the response)", errorEx);
            }
        }

        /// <summary>
        /// Shut down the Web Service
        /// </summary>
        public void Stop()
        {
            if (_listener != null)
            {
                _logger.Info("Stopping HttpListener...");
                _listener.Stop();
                _logger.Info("HttpListener stopped");
            }
        }

        private readonly Dictionary<string, int> _skipLogExtensions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {".js", 0},
            {".css", 0},
            {".woff", 0},
            {".woff2", 0},
            {".ttf", 0},
            {".html", 0}
        };

        private bool EnableLogging(string url, string localPath)
        {
            var extension = GetExtension(url);

            if (string.IsNullOrWhiteSpace(extension) || !_skipLogExtensions.ContainsKey(extension))
            {
                if (string.IsNullOrWhiteSpace(localPath) || localPath.IndexOf("system/ping", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetExtension(string url)
        {
            var parts = url.Split(new[] { '?' }, 2);

            return Path.GetExtension(parts[0]);
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
                ? String.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
                : pagePathWithoutQueryString;
        }

        private string GetUrlToLog(string url)
        {
            url = RemoveQueryStringByKey(url, "api_key");

            return url;
        }

        private string NormalizeConfiguredLocalAddress(string address)
        {
            var index = address.Trim('/').IndexOf('/');

            if (index != -1)
            {
                address = address.Substring(index + 1);
            }

            return address.Trim('/');
        }

        private bool ValidateHost(Uri url)
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

            var host = url.Host ?? string.Empty;

            _logger.Debug("Validating host {0}", host);

            if (_networkManager.IsInPrivateAddressSpace(host))
            {
                hosts.Add("localhost");
                hosts.Add("127.0.0.1");

                return hosts.Any(i => host.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1);
            }

            return true;
        }

        /// <summary>
        /// Overridable method that can be used to implement a custom hnandler
        /// </summary>
        protected async Task RequestHandler(IHttpRequest httpReq, Uri url, CancellationToken cancellationToken)
        {
            var date = DateTime.Now;
            var httpRes = httpReq.Response;
            bool enableLog = false;
            bool logHeaders = false;
            string urlToLog = null;
            string remoteIp = null;

            try
            {
                if (_disposed)
                {
                    httpRes.StatusCode = 503;
                    httpRes.ContentType = "text/plain";
                    Write(httpRes, "Server shutting down");
                    return;
                }

                if (!ValidateHost(url))
                {
                    httpRes.StatusCode = 400;
                    httpRes.ContentType = "text/plain";
                    Write(httpRes, "Invalid host");
                    return;
                }

                if (string.Equals(httpReq.Verb, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    httpRes.StatusCode = 200;
                    httpRes.AddHeader("Access-Control-Allow-Origin", "*");
                    httpRes.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
                    httpRes.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, Range, X-MediaBrowser-Token, X-Emby-Authorization");
                    httpRes.ContentType = "text/plain";
                    Write(httpRes, string.Empty);
                    return;
                }

                var operationName = httpReq.OperationName;
                var localPath = url.LocalPath;

                var urlString = url.OriginalString;
                enableLog = EnableLogging(urlString, localPath);
                urlToLog = urlString;
                 logHeaders = enableLog && urlToLog.IndexOf("/videos/", StringComparison.OrdinalIgnoreCase) != -1;

                if (enableLog)
                {
                    urlToLog = GetUrlToLog(urlString);
                    remoteIp = httpReq.RemoteIp;

                    LoggerUtils.LogRequest(_logger, urlToLog, httpReq.HttpMethod, httpReq.UserAgent, logHeaders ? httpReq.Headers : null);
                }

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

                if (string.Equals(localPath, "/mediabrowser/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(localPath, "/mediabrowser", StringComparison.OrdinalIgnoreCase) ||
                    localPath.IndexOf("mediabrowser/web", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    httpRes.StatusCode = 200;
                    httpRes.ContentType = "text/html";
                    var newUrl = urlString.Replace("mediabrowser", "emby", StringComparison.OrdinalIgnoreCase)
                        .Replace("/dashboard/", "/web/", StringComparison.OrdinalIgnoreCase);

                    if (!string.Equals(newUrl, urlString, StringComparison.OrdinalIgnoreCase))
                    {
                        Write(httpRes,
                            "<!doctype html><html><head><title>Emby</title></head><body>Please update your Emby bookmark to <a href=\"" +
                            newUrl + "\">" + newUrl + "</a></body></html>");
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
                        Write(httpRes,
                            "<!doctype html><html><head><title>Emby</title></head><body>Please update your Emby bookmark to <a href=\"" +
                            newUrl + "\">" + newUrl + "</a></body></html>");
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

                if (string.Equals(localPath, "/emby/pin", StringComparison.OrdinalIgnoreCase))
                {
                    RedirectToUrl(httpRes, "web/pin.html");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(GlobalResponse))
                {
                    httpRes.StatusCode = 503;
                    httpRes.ContentType = "text/html";
                    Write(httpRes, GlobalResponse);
                    return;
                }

                var handler = GetServiceHandler(httpReq);

                if (handler != null)
                {
                    await handler.ProcessRequestAsync(this, httpReq, httpRes, Logger, operationName, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    ErrorHandler(new FileNotFoundException(), httpReq, false);
                }
            }
            catch (OperationCanceledException ex)
            {
                ErrorHandler(ex, httpReq, false);
            }

            catch (Exception ex)
            {
                ErrorHandler(ex, httpReq, !string.Equals(ex.GetType().Name, "SocketException", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                httpRes.Close();

                if (enableLog)
                {
                    var statusCode = httpRes.StatusCode;

                    var duration = DateTime.Now - date;

                    LoggerUtils.LogResponse(_logger, statusCode, urlToLog, remoteIp, duration, logHeaders ? httpRes.Headers : null);
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
                _logger.Error("Path parts empty for PathInfo: {0}, Url: {1}", pathInfo, httpReq.RawUrl);
                return null;
            }

            string contentType;
            var restPath = ServiceHandler.FindMatchingRestPath(httpReq.HttpMethod, pathInfo, _logger, out contentType);

            if (restPath != null)
            {
                return new ServiceHandler
                {
                    RestPath = restPath,
                    ResponseContentType = contentType
                };
            }

            _logger.Error("Could not find handler for {0}", pathInfo);
            return null;
        }

        private void Write(IResponse response, string text)
        {
            var bOutput = Encoding.UTF8.GetBytes(text);
            response.SetContentLength(bOutput.Length);

            var outputStream = response.OutputStream;
            outputStream.Write(bOutput, 0, bOutput.Length);
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
        public void Init(IEnumerable<IService> services)
        {
            _restServices.AddRange(services);

            ServiceController = CreateServiceController();

            _logger.Info("Calling ServiceStack AppHost.Init");

            ServiceController.Init(this);

            var requestFilters = _appHost.GetExports<IRequestFilter>().ToList();
            foreach (var filter in requestFilters)
            {
                RequestFilters.Add(filter.Filter);
            }

            ResponseFilters.Add(new ResponseFilter(_logger).FilterResponse);
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

                routes.Add(new RouteAttribute(NormalizeRoutePath(route.Path), route.Verbs)
                {
                    Notes = route.Notes,
                    Priority = route.Priority,
                    Summary = route.Summary
                });

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

        public object DeserializeXml(Type type, Stream stream)
        {
            return _xmlSerializer.DeserializeFromStream(type, stream);
        }

        public object DeserializeJson(Type type, Stream stream)
        {
            return _jsonSerializer.DeserializeFromStream(stream, type);
        }

        private string NormalizeEmbyRoutePath(string path)
        {
            if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return "/emby" + path;
            }

            return "emby/" + path;
        }

        private string DoubleNormalizeEmbyRoutePath(string path)
        {
            if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return "/emby/emby" + path;
            }

            return "emby/emby/" + path;
        }

        private string NormalizeRoutePath(string path)
        {
            if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return "/mediabrowser" + path;
            }

            return "mediabrowser/" + path;
        }

        private bool _disposed;
        private readonly object _disposeLock = new object();
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void StartServer(IEnumerable<string> urlPrefixes)
        {
            UrlPrefixes = urlPrefixes.ToList();
            StartListener();
        }
    }
}