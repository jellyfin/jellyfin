using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Funq;
using MediaBrowser.Model.Logging;
using ServiceStack;
using ServiceStack.Host;
using ServiceStack.Web;
using SocketHttpListener.Net;

namespace MediaBrowser.Server.Implementations.HttpServer.SocketSharp
{
    public partial class WebSocketSharpRequest : IHttpRequest
    {
        public Container Container { get; set; }
        private readonly HttpListenerRequest request;
        private readonly IHttpResponse response;

        public WebSocketSharpRequest(HttpListenerContext httpContext, string operationName, RequestAttributes requestAttributes, ILogger logger)
        {
            this.OperationName = operationName;
            this.RequestAttributes = requestAttributes;
            this.request = httpContext.Request;
            this.response = new WebSocketSharpResponse(logger, httpContext.Response, this);

            this.RequestPreferences = new RequestPreferences(this);
        }

        public HttpListenerRequest HttpRequest
        {
            get { return request; }
        }

        public object OriginalRequest
        {
            get { return request; }
        }

        public IResponse Response
        {
            get { return response; }
        }

        public IHttpResponse HttpResponse
        {
            get { return response; }
        }

        public RequestAttributes RequestAttributes { get; set; }

        public IRequestPreferences RequestPreferences { get; private set; }

        public T TryResolve<T>()
        {
            if (typeof(T) == typeof(IHttpRequest))
                throw new Exception("You don't need to use IHttpRequest.TryResolve<IHttpRequest> to resolve itself");

            if (typeof(T) == typeof(IHttpResponse))
                throw new Exception("Resolve IHttpResponse with 'Response' property instead of IHttpRequest.TryResolve<IHttpResponse>");

            return Container == null
                ? HostContext.TryResolve<T>()
                : Container.TryResolve<T>();
        }

        public string OperationName { get; set; }

        public object Dto { get; set; }

        public string GetRawBody()
        {
            if (bufferedStream != null)
            {
                return bufferedStream.ToArray().FromUtf8Bytes();
            }

            using (var reader = new StreamReader(InputStream))
            {
                return reader.ReadToEnd();
            }
        }

        public string RawUrl
        {
            get { return request.RawUrl; }
        }

        public string AbsoluteUri
        {
            get { return request.Url.AbsoluteUri.TrimEnd('/'); }
        }

        public string UserHostAddress
        {
            get { return request.UserHostAddress; }
        }

        public string XForwardedFor
        {
            get
            {
                return String.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedFor]) ? null : request.Headers[HttpHeaders.XForwardedFor];
            }
        }

        public int? XForwardedPort
        {
            get
            {
                return string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedPort]) ? (int?)null : int.Parse(request.Headers[HttpHeaders.XForwardedPort]);
            }
        }

        public string XForwardedProtocol
        {
            get
            {
                return string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedProtocol]) ? null : request.Headers[HttpHeaders.XForwardedProtocol];
            }
        }

        public string XRealIp
        {
            get
            {
                return String.IsNullOrEmpty(request.Headers[HttpHeaders.XRealIp]) ? null : request.Headers[HttpHeaders.XRealIp];
            }
        }

        private string remoteIp;
        public string RemoteIp
        {
            get
            {
                return remoteIp ??
                    (remoteIp = (CheckBadChars(XForwardedFor)) ??
                                (NormalizeIp(CheckBadChars(XRealIp)) ??
                                (request.RemoteEndPoint != null ? NormalizeIp(request.RemoteEndPoint.Address.ToString()) : null)));
            }
        }

        private static readonly char[] HttpTrimCharacters = new char[] { (char)0x09, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20 };

        //
        // CheckBadChars - throws on invalid chars to be not found in header name/value
        //
        internal static string CheckBadChars(string name)
        {
            if (name == null || name.Length == 0)
            {
                return name;
            }

            // VALUE check
            //Trim spaces from both ends
            name = name.Trim(HttpTrimCharacters);

            //First, check for correctly formed multi-line value
            //Second, check for absenece of CTL characters
            int crlf = 0;
            for (int i = 0; i < name.Length; ++i)
            {
                char c = (char)(0x000000ff & (uint)name[i]);
                switch (crlf)
                {
                    case 0:
                        if (c == '\r')
                        {
                            crlf = 1;
                        }
                        else if (c == '\n')
                        {
                            // Technically this is bad HTTP.  But it would be a breaking change to throw here.
                            // Is there an exploit?
                            crlf = 2;
                        }
                        else if (c == 127 || (c < ' ' && c != '\t'))
                        {
                            throw new ArgumentException("net_WebHeaderInvalidControlChars");
                        }
                        break;

                    case 1:
                        if (c == '\n')
                        {
                            crlf = 2;
                            break;
                        }
                        throw new ArgumentException("net_WebHeaderInvalidCRLFChars");

                    case 2:
                        if (c == ' ' || c == '\t')
                        {
                            crlf = 0;
                            break;
                        }
                        throw new ArgumentException("net_WebHeaderInvalidCRLFChars");
                }
            }
            if (crlf != 0)
            {
                throw new ArgumentException("net_WebHeaderInvalidCRLFChars");
            }
            return name;
        }

        internal static bool ContainsNonAsciiChars(string token)
        {
            for (int i = 0; i < token.Length; ++i)
            {
                if ((token[i] < 0x20) || (token[i] > 0x7e))
                {
                    return true;
                }
            }
            return false;
        }

        private string NormalizeIp(string ip)
        {
            if (!string.IsNullOrWhiteSpace(ip))
            {
                // Handle ipv4 mapped to ipv6
                const string srch = "::ffff:";
                var index = ip.IndexOf(srch, StringComparison.OrdinalIgnoreCase);
                if (index == 0)
                {
                    ip = ip.Substring(srch.Length);
                }
            }

            return ip;
        }

        public bool IsSecureConnection
        {
            get { return request.IsSecureConnection || XForwardedProtocol == "https"; }
        }

        public string[] AcceptTypes
        {
            get { return request.AcceptTypes; }
        }

        private Dictionary<string, object> items;
        public Dictionary<string, object> Items
        {
            get { return items ?? (items = new Dictionary<string, object>()); }
        }

        private string responseContentType;
        public string ResponseContentType
        {
            get
            {
                return responseContentType
                    ?? (responseContentType = this.GetResponseContentType());
            }
            set
            {
                this.responseContentType = value;
                HasExplicitResponseContentType = true;
            }
        }

        public bool HasExplicitResponseContentType { get; private set; }

        private string pathInfo;
        public string PathInfo
        {
            get
            {
                if (this.pathInfo == null)
                {
                    var mode = HostContext.Config.HandlerFactoryPath;

                    var pos = request.RawUrl.IndexOf("?");
                    if (pos != -1)
                    {
                        var path = request.RawUrl.Substring(0, pos);
                        this.pathInfo = HttpRequestExtensions.GetPathInfo(
                            path,
                            mode,
                            mode ?? "");
                    }
                    else
                    {
                        this.pathInfo = request.RawUrl;
                    }

                    this.pathInfo = this.pathInfo.UrlDecode();
                    this.pathInfo = NormalizePathInfo(pathInfo, mode);
                }
                return this.pathInfo;
            }
        }

        private Dictionary<string, System.Net.Cookie> cookies;
        public IDictionary<string, System.Net.Cookie> Cookies
        {
            get
            {
                if (cookies == null)
                {
                    cookies = new Dictionary<string, System.Net.Cookie>();
                    for (var i = 0; i < this.request.Cookies.Count; i++)
                    {
                        var httpCookie = this.request.Cookies[i];
                        cookies[httpCookie.Name] = new System.Net.Cookie(httpCookie.Name, httpCookie.Value, httpCookie.Path, httpCookie.Domain);
                    }
                }

                return cookies;
            }
        }

        public string UserAgent
        {
            get { return request.UserAgent; }
        }

        private NameValueCollectionWrapper headers;
        public INameValueCollection Headers
        {
            get { return headers ?? (headers = new NameValueCollectionWrapper(request.Headers)); }
        }

        private NameValueCollectionWrapper queryString;
        public INameValueCollection QueryString
        {
            get { return queryString ?? (queryString = new NameValueCollectionWrapper(MyHttpUtility.ParseQueryString(request.Url.Query))); }
        }

        private NameValueCollectionWrapper formData;
        public INameValueCollection FormData
        {
            get { return formData ?? (formData = new NameValueCollectionWrapper(this.Form)); }
        }

        public bool IsLocal
        {
            get { return request.IsLocal; }
        }

        private string httpMethod;
        public string HttpMethod
        {
            get
            {
                return httpMethod
                    ?? (httpMethod = Param(HttpHeaders.XHttpMethodOverride)
                    ?? request.HttpMethod);
            }
        }

        public string Verb
        {
            get { return HttpMethod; }
        }

        public string Param(string name)
        {
            return Headers[name]
                ?? QueryString[name]
                ?? FormData[name];
        }

        public string ContentType
        {
            get { return request.ContentType; }
        }

        public Encoding contentEncoding;
        public Encoding ContentEncoding
        {
            get { return contentEncoding ?? request.ContentEncoding; }
            set { contentEncoding = value; }
        }

        public Uri UrlReferrer
        {
            get { return request.UrlReferrer; }
        }

        public static Encoding GetEncoding(string contentTypeHeader)
        {
            var param = GetParameter(contentTypeHeader, "charset=");
            if (param == null) return null;
            try
            {
                return Encoding.GetEncoding(param);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public bool UseBufferedStream
        {
            get { return bufferedStream != null; }
            set
            {
                bufferedStream = value
                    ? bufferedStream ?? new MemoryStream(request.InputStream.ReadFully())
                    : null;
            }
        }

        private MemoryStream bufferedStream;
        public Stream InputStream
        {
            get { return bufferedStream ?? request.InputStream; }
        }

        public long ContentLength
        {
            get { return request.ContentLength64; }
        }

        private IHttpFile[] httpFiles;
        public IHttpFile[] Files
        {
            get
            {
                if (httpFiles == null)
                {
                    if (files == null)
                        return httpFiles = new IHttpFile[0];

                    httpFiles = new IHttpFile[files.Count];
                    for (var i = 0; i < files.Count; i++)
                    {
                        var reqFile = files[i];

                        httpFiles[i] = new HttpFile
                        {
                            ContentType = reqFile.ContentType,
                            ContentLength = reqFile.ContentLength,
                            FileName = reqFile.FileName,
                            InputStream = reqFile.InputStream,
                        };
                    }
                }
                return httpFiles;
            }
        }

        static Stream GetSubStream(Stream stream)
        {
            if (stream is MemoryStream)
            {
                var other = (MemoryStream)stream;
                try
                {
                    return new MemoryStream(other.GetBuffer(), 0, (int)other.Length, false, true);
                }
                catch (UnauthorizedAccessException)
                {
                    return new MemoryStream(other.ToArray(), 0, (int)other.Length, false, true);
                }
            }

            return stream;
        }

        public static string GetHandlerPathIfAny(string listenerUrl)
        {
            if (listenerUrl == null) return null;
            var pos = listenerUrl.IndexOf("://", StringComparison.InvariantCultureIgnoreCase);
            if (pos == -1) return null;
            var startHostUrl = listenerUrl.Substring(pos + "://".Length);
            var endPos = startHostUrl.IndexOf('/');
            if (endPos == -1) return null;
            var endHostUrl = startHostUrl.Substring(endPos + 1);
            return String.IsNullOrEmpty(endHostUrl) ? null : endHostUrl.TrimEnd('/');
        }

        public static string NormalizePathInfo(string pathInfo, string handlerPath)
        {
            if (handlerPath != null && pathInfo.TrimStart('/').StartsWith(
                handlerPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return pathInfo.TrimStart('/').Substring(handlerPath.Length);
            }

            return pathInfo;
        }
    }
}
