using Funq;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.HttpServer.SocketSharp;
using ServiceStack;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Emby.Common.Implementations.Net;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.HttpServer.SocketSharp;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Text;
using SocketHttpListener.Net;
using SocketHttpListener.Primitives;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public class HttpListenerHost : ServiceStackHost, IHttpServer
    {
        private string DefaultRedirectPath { get; set; }

        private readonly ILogger _logger;
        public IEnumerable<string> UrlPrefixes { get; private set; }

        private readonly List<IService> _restServices = new List<IService>();

        private IHttpListener _listener;

        private readonly ContainerAdapter _containerAdapter;

        public event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;
        public event EventHandler<WebSocketConnectingEventArgs> WebSocketConnecting;

        public string CertificatePath { get; private set; }

        private readonly IServerConfigurationManager _config;
        private readonly INetworkManager _networkManager;
        private readonly IMemoryStreamFactory _memoryStreamProvider;

        private readonly IServerApplicationHost _appHost;

        private readonly ITextEncoding _textEncoding;
        private readonly ISocketFactory _socketFactory;
        private readonly ICryptoProvider _cryptoProvider;

        public HttpListenerHost(IServerApplicationHost applicationHost,
            ILogManager logManager,
            IServerConfigurationManager config,
            string serviceName,
            string defaultRedirectPath, INetworkManager networkManager, IMemoryStreamFactory memoryStreamProvider, ITextEncoding textEncoding, ISocketFactory socketFactory, ICryptoProvider cryptoProvider)
            : base(serviceName, new Assembly[] { })
        {
            _appHost = applicationHost;
            DefaultRedirectPath = defaultRedirectPath;
            _networkManager = networkManager;
            _memoryStreamProvider = memoryStreamProvider;
            _textEncoding = textEncoding;
            _socketFactory = socketFactory;
            _cryptoProvider = cryptoProvider;
            _config = config;

            _logger = logManager.GetLogger("HttpServer");

            _containerAdapter = new ContainerAdapter(applicationHost);
        }

        public string GlobalResponse { get; set; }

        public override void Configure()
        {
            HostConfig.Instance.DefaultRedirectPath = DefaultRedirectPath;

            HostConfig.Instance.MapExceptionToStatusCode = new Dictionary<Type, int>
            {
                {typeof (InvalidOperationException), 500},
                {typeof (NotImplementedException), 500},
                {typeof (ResourceNotFoundException), 404},
                {typeof (FileNotFoundException), 404},
                {typeof (DirectoryNotFoundException), 404},
                {typeof (SecurityException), 401},
                {typeof (PaymentRequiredException), 402},
                {typeof (UnauthorizedAccessException), 500},
                {typeof (ApplicationException), 500},
                {typeof (PlatformNotSupportedException), 500},
                {typeof (NotSupportedException), 500}
            };

            // The Markdown feature causes slow startup times (5 mins+) on cold boots for some users
            // Custom format allows images
            HostConfig.Instance.EnableFeatures = Feature.Html | Feature.Json | Feature.Xml | Feature.CustomFormat;

            Container.Adapter = _containerAdapter;

            var requestFilters = _appHost.GetExports<IRequestFilter>().ToList();
            foreach (var filter in requestFilters)
            {
                HostContext.GlobalRequestFilters.Add(filter.Filter);
            }

            HostContext.GlobalResponseFilters.Add(new ResponseFilter(_logger).FilterResponse);
        }

        protected override ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();

            Config.HandlerFactoryPath = null;

            Config.MetadataRedirectPath = "metadata";
        }

        protected override ServiceController CreateServiceController(params Assembly[] assembliesWithServices)
        {
            var types = _restServices.Select(r => r.GetType()).ToArray();

            return new ServiceController(this, () => types);
        }

        public override ServiceStackHost Start(string listeningAtUrlBase)
        {
            StartListener();
            return this;
        }

        /// <summary>
        /// Starts the Web Service
        /// </summary>
        private void StartListener()
        {
            HostContext.Config.HandlerFactoryPath = GetHandlerPathIfAny(UrlPrefixes.First());

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
            var cert = !string.IsNullOrWhiteSpace(CertificatePath) && File.Exists(CertificatePath)
                ? GetCert(CertificatePath) :
                null;

            var enableDualMode = Environment.OSVersion.Platform == PlatformID.Win32NT;

            return new WebSocketSharpListener(_logger, cert, _memoryStreamProvider, _textEncoding, _networkManager, _socketFactory, _cryptoProvider, new StreamFactory(), enableDualMode, GetRequest);
        }

        public static ICertificate GetCert(string certificateLocation)
        {
            X509Certificate2 localCert = new X509Certificate2(certificateLocation);
            //localCert.PrivateKey = PrivateKey.CreateFromFile(pvk_file).RSA;
            if (localCert.PrivateKey == null)
            {
                //throw new FileNotFoundException("Secure requested, no private key included", certificateLocation);
                return null;
            }

            return new Certificate(localCert);
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

        private void ErrorHandler(Exception ex, IRequest httpReq)
        {
            try
            {
                var httpRes = httpReq.Response;

                if (httpRes.IsClosed)
                {
                    return;
                }

                var errorResponse = new ErrorResponse
                {
                    ResponseStatus = new ResponseStatus
                    {
                        ErrorCode = ex.GetType().GetOperationName(),
                        Message = ex.Message,
                        StackTrace = ex.StackTrace
                    }
                };

                var contentType = httpReq.ResponseContentType;

                var serializer = ContentTypes.Instance.GetResponseSerializer(contentType);
                if (serializer == null)
                {
                    contentType = HostContext.Config.DefaultContentType;
                    serializer = ContentTypes.Instance.GetResponseSerializer(contentType);
                }

                var httpError = ex as IHttpError;
                if (httpError != null)
                {
                    httpRes.StatusCode = httpError.Status;
                    httpRes.StatusDescription = httpError.StatusDescription;
                }
                else
                {
                    httpRes.StatusCode = 500;
                }

                httpRes.ContentType = contentType;

                serializer(httpReq, errorResponse, httpRes);

                httpRes.Close();
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
                _listener.Stop();
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

            if (newQueryString.Count == 0)
            {
                return url;
            }

            // this removes the key if exists
            newQueryString.Remove(key);

            // this gets the page path from root without QueryString
            string pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

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
        /// <param name="httpReq">The HTTP req.</param>
        /// <param name="url">The URL.</param>
        /// <returns>Task.</returns>
        protected async Task RequestHandler(IHttpRequest httpReq, Uri url)
        {
            var date = DateTime.Now;
            var httpRes = httpReq.Response;
            bool enableLog = false;
            string urlToLog = null;
            string remoteIp = null;

            try
            {
                if (_disposed)
                {
                    httpRes.StatusCode = 503;
                    return;
                }

                if (!ValidateHost(url))
                {
                    httpRes.StatusCode = 400;
                    httpRes.ContentType = "text/plain";
                    httpRes.Write("Invalid host");
                    return;
                }

                if (string.Equals(httpReq.Verb, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    httpRes.StatusCode = 200;
                    httpRes.AddHeader("Access-Control-Allow-Origin", "*");
                    httpRes.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
                    httpRes.AddHeader("Access-Control-Allow-Headers",
                        "Content-Type, Authorization, Range, X-MediaBrowser-Token, X-Emby-Authorization");
                    httpRes.ContentType = "text/html";
                    return;
                }

                var operationName = httpReq.OperationName;
                var localPath = url.LocalPath;

                var urlString = url.OriginalString;
                enableLog = EnableLogging(urlString, localPath);
                urlToLog = urlString;

                if (enableLog)
                {
                    urlToLog = GetUrlToLog(urlString);
                    remoteIp = httpReq.RemoteIp;

                    LoggerUtils.LogRequest(_logger, urlToLog, httpReq.HttpMethod, httpReq.UserAgent);
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
                        httpRes.Write(
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
                        httpRes.Write(
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
                    httpRes.Write(GlobalResponse);
                    return;
                }

                var handler = HttpHandlerFactory.GetHandler(httpReq);

                if (handler != null)
                {
                    await handler.ProcessRequestAsync(httpReq, httpRes, operationName).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler(ex, httpReq);
            }
            finally
            {
                httpRes.Close();

                if (enableLog)
                {
                    var statusCode = httpRes.StatusCode;

                    var duration = DateTime.Now - date;

                    LoggerUtils.LogResponse(_logger, statusCode, urlToLog, remoteIp, duration);
                }
            }
        }

        public static void RedirectToUrl(IResponse httpRes, string url)
        {
            httpRes.StatusCode = 302;
            httpRes.AddHeader(HttpHeaders.Location, url);
            httpRes.EndRequest();
        }


        /// <summary>
        /// Adds the rest handlers.
        /// </summary>
        /// <param name="services">The services.</param>
        public void Init(IEnumerable<IService> services)
        {
            _restServices.AddRange(services);

            ServiceController = CreateServiceController();

            _logger.Info("Calling ServiceStack AppHost.Init");

            base.Init();
        }

        public override Model.Services.RouteAttribute[] GetRouteAttributes(Type requestType)
        {
            var routes = base.GetRouteAttributes(requestType).ToList();
            var clone = routes.ToList();

            foreach (var route in clone)
            {
                routes.Add(new Model.Services.RouteAttribute(NormalizeEmbyRoutePath(route.Path), route.Verbs)
                {
                    Notes = route.Notes,
                    Priority = route.Priority,
                    Summary = route.Summary
                });

                routes.Add(new Model.Services.RouteAttribute(NormalizeRoutePath(route.Path), route.Verbs)
                {
                    Notes = route.Notes,
                    Priority = route.Priority,
                    Summary = route.Summary
                });

                routes.Add(new Model.Services.RouteAttribute(DoubleNormalizeEmbyRoutePath(route.Path), route.Verbs)
                {
                    Notes = route.Notes,
                    Priority = route.Priority,
                    Summary = route.Summary
                });
            }

            return routes.ToArray();
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
            base.Dispose();

            lock (_disposeLock)
            {
                if (_disposed) return;

                if (disposing)
                {
                    Stop();
                }

                //release unmanaged resources here...
                _disposed = true;
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void StartServer(IEnumerable<string> urlPrefixes, string certificatePath)
        {
            CertificatePath = certificatePath;
            UrlPrefixes = urlPrefixes.ToList();
            Start(UrlPrefixes.First());
        }
    }

    public class StreamFactory : IStreamFactory
    {
        public Stream CreateNetworkStream(ISocket socket, bool ownsSocket)
        {
            var netSocket = (NetSocket)socket;

            return new NetworkStream(netSocket.Socket, ownsSocket);
        }

        public Task AuthenticateSslStreamAsServer(Stream stream, ICertificate certificate)
        {
            var sslStream = (SslStream)stream;
            var cert = (Certificate)certificate;

            return sslStream.AuthenticateAsServerAsync(cert.X509Certificate);
        }

        public Stream CreateSslStream(Stream innerStream, bool leaveInnerStreamOpen)
        {
            return new SslStream(innerStream, leaveInnerStreamOpen);
        }
    }

    public class Certificate : ICertificate
    {
        public Certificate(X509Certificate x509Certificate)
        {
            X509Certificate = x509Certificate;
        }

        public X509Certificate X509Certificate { get; private set; }
    }
}