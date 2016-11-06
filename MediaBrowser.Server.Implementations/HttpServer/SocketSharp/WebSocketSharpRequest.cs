using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Emby.Server.Implementations.HttpServer.SocketSharp;
using Funq;
using MediaBrowser.Common.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using ServiceStack;
using ServiceStack.Host;
using ServiceStack.Web;
using SocketHttpListener.Net;
using IHttpFile = MediaBrowser.Model.Services.IHttpFile;
using IHttpRequest = MediaBrowser.Model.Services.IHttpRequest;
using IHttpResponse = MediaBrowser.Model.Services.IHttpResponse;
using IResponse = MediaBrowser.Model.Services.IResponse;

namespace MediaBrowser.Server.Implementations.HttpServer.SocketSharp
{
    public partial class WebSocketSharpRequest : IHttpRequest
    {
        public Container Container { get; set; }
        private readonly HttpListenerRequest request;
        private readonly IHttpResponse response;
        private readonly IMemoryStreamProvider _memoryStreamProvider;

        public WebSocketSharpRequest(HttpListenerContext httpContext, string operationName, RequestAttributes requestAttributes, ILogger logger, IMemoryStreamProvider memoryStreamProvider)
        {
            this.OperationName = operationName;
            _memoryStreamProvider = memoryStreamProvider;
            this.request = httpContext.Request;
            this.response = new WebSocketSharpResponse(logger, httpContext.Response, this);
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
                    ?? (responseContentType = GetResponseContentType(this));
            }
            set
            {
                this.responseContentType = value;
                HasExplicitResponseContentType = true;
            }
        }

        private static string GetResponseContentType(IRequest httpReq)
        {
            var specifiedContentType = GetQueryStringContentType(httpReq);
            if (!string.IsNullOrEmpty(specifiedContentType)) return specifiedContentType;

            var acceptContentTypes = httpReq.AcceptTypes;
            var defaultContentType = httpReq.ContentType;
            if (httpReq.HasAnyOfContentTypes(MimeTypes.FormUrlEncoded, MimeTypes.MultiPartFormData))
            {
                defaultContentType = HostContext.Config.DefaultContentType;
            }

            var customContentTypes = HostContext.ContentTypes.ContentTypeFormats.Values;
            var preferredContentTypes = new string[] {};

            var acceptsAnything = false;
            var hasDefaultContentType = !string.IsNullOrEmpty(defaultContentType);
            if (acceptContentTypes != null)
            {
                var hasPreferredContentTypes = new bool[preferredContentTypes.Length];
                foreach (var acceptsType in acceptContentTypes)
                {
                    var contentType = ContentFormat.GetRealContentType(acceptsType);
                    acceptsAnything = acceptsAnything || contentType == "*/*";

                    for (var i = 0; i < preferredContentTypes.Length; i++)
                    {
                        if (hasPreferredContentTypes[i]) continue;
                        var preferredContentType = preferredContentTypes[i];
                        hasPreferredContentTypes[i] = contentType.StartsWith(preferredContentType);

                        //Prefer Request.ContentType if it is also a preferredContentType
                        if (hasPreferredContentTypes[i] && preferredContentType == defaultContentType)
                            return preferredContentType;
                    }
                }

                for (var i = 0; i < preferredContentTypes.Length; i++)
                {
                    if (hasPreferredContentTypes[i]) return preferredContentTypes[i];
                }

                if (acceptsAnything)
                {
                    if (hasDefaultContentType)
                        return defaultContentType;
                    if (HostContext.Config.DefaultContentType != null)
                        return HostContext.Config.DefaultContentType;
                }

                foreach (var contentType in acceptContentTypes)
                {
                    foreach (var customContentType in customContentTypes)
                    {
                        if (contentType.StartsWith(customContentType, StringComparison.OrdinalIgnoreCase))
                            return customContentType;
                    }
                }
            }

            if (httpReq.ContentType.MatchesContentType(MimeTypes.Soap12))
            {
                return MimeTypes.Soap12;
            }

            if (acceptContentTypes == null && httpReq.ContentType == MimeTypes.Soap11)
            {
                return MimeTypes.Soap11;
            }

            //We could also send a '406 Not Acceptable', but this is allowed also
            return HostContext.Config.DefaultContentType;
        }

        private static string GetQueryStringContentType(IRequest httpReq)
        {
            var callback = httpReq.QueryString[Keywords.Callback];
            if (!string.IsNullOrEmpty(callback)) return MimeTypes.Json;

            var format = httpReq.QueryString[Keywords.Format];
            if (format == null)
            {
                const int formatMaxLength = 4;
                var pi = httpReq.PathInfo;
                if (pi == null || pi.Length <= formatMaxLength) return null;
                if (pi[0] == '/') pi = pi.Substring(1);
                format = pi.LeftPart('/');
                if (format.Length > formatMaxLength) return null;
            }

            format = format.LeftPart('.').ToLower();
            if (format.Contains("json")) return MimeTypes.Json;
            if (format.Contains("xml")) return MimeTypes.Xml;
            if (format.Contains("jsv")) return MimeTypes.Jsv;

            string contentType;
            HostContext.ContentTypes.ContentTypeFormats.TryGetValue(format, out contentType);

            return contentType;
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
                        this.pathInfo = GetPathInfo(
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

        private static string GetPathInfo(string fullPath, string mode, string appPath)
        {
            var pathInfo = ResolvePathInfoFromMappedPath(fullPath, mode);
            if (!string.IsNullOrEmpty(pathInfo)) return pathInfo;

            //Wildcard mode relies on this to work out the handlerPath
            pathInfo = ResolvePathInfoFromMappedPath(fullPath, appPath);
            if (!string.IsNullOrEmpty(pathInfo)) return pathInfo;

            return fullPath;
        }



        private static string ResolvePathInfoFromMappedPath(string fullPath, string mappedPathRoot)
        {
            if (mappedPathRoot == null) return null;

            var sbPathInfo = new StringBuilder();
            var fullPathParts = fullPath.Split('/');
            var mappedPathRootParts = mappedPathRoot.Split('/');
            var fullPathIndexOffset = mappedPathRootParts.Length - 1;
            var pathRootFound = false;

            for (var fullPathIndex = 0; fullPathIndex < fullPathParts.Length; fullPathIndex++)
            {
                if (pathRootFound)
                {
                    sbPathInfo.Append("/" + fullPathParts[fullPathIndex]);
                }
                else if (fullPathIndex - fullPathIndexOffset >= 0)
                {
                    pathRootFound = true;
                    for (var mappedPathRootIndex = 0; mappedPathRootIndex < mappedPathRootParts.Length; mappedPathRootIndex++)
                    {
                        if (!string.Equals(fullPathParts[fullPathIndex - fullPathIndexOffset + mappedPathRootIndex], mappedPathRootParts[mappedPathRootIndex], StringComparison.OrdinalIgnoreCase))
                        {
                            pathRootFound = false;
                            break;
                        }
                    }
                }
            }
            if (!pathRootFound) return null;

            var path = sbPathInfo.ToString();
            return path.Length > 1 ? path.TrimEnd('/') : "/";
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

        private QueryParamCollection headers;
        public QueryParamCollection Headers
        {
            get { return headers ?? (headers = ToQueryParams(request.Headers)); }
        }

        private QueryParamCollection queryString;
        public QueryParamCollection QueryString
        {
            get { return queryString ?? (queryString = MyHttpUtility.ParseQueryString(request.Url.Query)); }
        }

        private QueryParamCollection formData;
        public QueryParamCollection FormData
        {
            get { return formData ?? (formData = this.Form); }
        }

        private QueryParamCollection ToQueryParams(NameValueCollection collection)
        {
            var result = new QueryParamCollection();

            foreach (var key in collection.AllKeys)
            {
                result[key] = collection[key];
            }

            return result;
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
                    ? bufferedStream ?? _memoryStreamProvider.CreateNew(request.InputStream.ReadFully())
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

        static Stream GetSubStream(Stream stream, IMemoryStreamProvider streamProvider)
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
