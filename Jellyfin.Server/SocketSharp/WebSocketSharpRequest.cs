using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Emby.Server.Implementations.HttpServer;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using SocketHttpListener.Net;
using IHttpFile = MediaBrowser.Model.Services.IHttpFile;
using IHttpRequest = MediaBrowser.Model.Services.IHttpRequest;
using IHttpResponse = MediaBrowser.Model.Services.IHttpResponse;
using IResponse = MediaBrowser.Model.Services.IResponse;

namespace Jellyfin.Server.SocketSharp
{
    public partial class WebSocketSharpRequest : IHttpRequest
    {
        private readonly HttpListenerRequest request;
        private readonly IHttpResponse response;

        public WebSocketSharpRequest(HttpListenerContext httpContext, string operationName, ILogger logger)
        {
            this.OperationName = operationName;
            this.request = httpContext.Request;
            this.response = new WebSocketSharpResponse(logger, httpContext.Response, this);

            //HandlerFactoryPath = GetHandlerPathIfAny(UrlPrefixes[0]);
        }

        public HttpListenerRequest HttpRequest => request;

        public object OriginalRequest => request;

        public IResponse Response => response;

        public IHttpResponse HttpResponse => response;

        public string OperationName { get; set; }

        public object Dto { get; set; }

        public string RawUrl => request.RawUrl;

        public string AbsoluteUri => request.Url.AbsoluteUri.TrimEnd('/');

        public string UserHostAddress => request.UserHostAddress;

        public string XForwardedFor => string.IsNullOrEmpty(request.Headers["X-Forwarded-For"]) ? null : request.Headers["X-Forwarded-For"];

        public int? XForwardedPort => string.IsNullOrEmpty(request.Headers["X-Forwarded-Port"]) ? (int?)null : int.Parse(request.Headers["X-Forwarded-Port"]);

        public string XForwardedProtocol => string.IsNullOrEmpty(request.Headers["X-Forwarded-Proto"]) ? null : request.Headers["X-Forwarded-Proto"];

        public string XRealIp => string.IsNullOrEmpty(request.Headers["X-Real-IP"]) ? null : request.Headers["X-Real-IP"];

        private string remoteIp;
        public string RemoteIp =>
            remoteIp ??
            (remoteIp = CheckBadChars(XForwardedFor) ??
                        NormalizeIp(CheckBadChars(XRealIp) ??
                         (request.RemoteEndPoint != null ? NormalizeIp(request.RemoteEndPoint.Address.ToString()) : null)));

        private static readonly char[] HttpTrimCharacters = new char[] { (char)0x09, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20 };

        // CheckBadChars - throws on invalid chars to be not found in header name/value
        internal static string CheckBadChars(string name)
        {
            if (name == null || name.Length == 0)
            {
                return name;
            }

            // VALUE check
            // Trim spaces from both ends
            name = name.Trim(HttpTrimCharacters);

            // First, check for correctly formed multi-line value
            // Second, check for absence of CTL characters
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

        public bool IsSecureConnection => request.IsSecureConnection || XForwardedProtocol == "https";

        public string[] AcceptTypes => request.AcceptTypes;

        private Dictionary<string, object> items;
        public Dictionary<string, object> Items => items ?? (items = new Dictionary<string, object>());

        private string responseContentType;
        public string ResponseContentType
        {
            get =>
                responseContentType
                ?? (responseContentType = GetResponseContentType(this));
            set => this.responseContentType = value;
        }

        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        public const string MultiPartFormData = "multipart/form-data";
        public static string GetResponseContentType(IRequest httpReq)
        {
            var specifiedContentType = GetQueryStringContentType(httpReq);
            if (!string.IsNullOrEmpty(specifiedContentType))
            {
                return specifiedContentType;
            }

            const string serverDefaultContentType = "application/json";

            var acceptContentTypes = httpReq.AcceptTypes;
            string defaultContentType = null;
            if (HasAnyOfContentTypes(httpReq, FormUrlEncoded, MultiPartFormData))
            {
                defaultContentType = serverDefaultContentType;
            }

            var acceptsAnything = false;
            var hasDefaultContentType = defaultContentType != null;
            if (acceptContentTypes != null)
            {
                foreach (var acceptsType in acceptContentTypes)
                {
                    // TODO: @bond move to Span when Span.Split lands
                    // https://github.com/dotnet/corefx/issues/26528
                    var contentType = acceptsType?.Split(';')[0];
                    acceptsAnything = contentType.IndexOf("*/*", StringComparison.Ordinal) != -1;

                    if (acceptsAnything)
                    {
                        break;
                    }
                }

                if (acceptsAnything)
                {
                    if (hasDefaultContentType)
                    {
                        return defaultContentType;
                    }
                    else
                    {
                        return serverDefaultContentType;
                    }
                }
            }

            if (acceptContentTypes == null && httpReq.ContentType == Soap11)
            {
                return Soap11;
            }

            // We could also send a '406 Not Acceptable', but this is allowed also
            return serverDefaultContentType;
        }

        public const string Soap11 = "text/xml; charset=utf-8";

        public static bool HasAnyOfContentTypes(IRequest request, params string[] contentTypes)
        {
            if (contentTypes == null || request.ContentType == null)
            {
                return false;
            }

            foreach (var contentType in contentTypes)
            {
                if (IsContentType(request, contentType))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsContentType(IRequest request, string contentType)
        {
            return request.ContentType.StartsWith(contentType, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetQueryStringContentType(IRequest httpReq)
        {
            ReadOnlySpan<char> format = httpReq.QueryString["format"];
            if (format == null)
            {
                const int formatMaxLength = 4;
                ReadOnlySpan<char> pi = httpReq.PathInfo;
                if (pi == null || pi.Length <= formatMaxLength)
                {
                    return null;
                }

                if (pi[0] == '/')
                {
                    pi = pi.Slice(1);
                }

                format = LeftPart(pi, '/');
                if (format.Length > formatMaxLength)
                {
                    return null;
                }
            }

            format = LeftPart(format, '.');
            if (format.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                return "application/json";
            }
            else if (format.Contains("xml", StringComparison.OrdinalIgnoreCase))
            {
                return "application/xml";
            }

            return null;
        }

        public static string LeftPart(string strVal, char needle)
        {
            if (strVal == null)
            {
                return null;
            }

            var pos = strVal.IndexOf(needle, StringComparison.Ordinal);
            return pos == -1 ? strVal : strVal.Substring(0, pos);
        }

        public static ReadOnlySpan<char> LeftPart(ReadOnlySpan<char> strVal, char needle)
        {
            if (strVal == null)
            {
                return null;
            }

            var pos = strVal.IndexOf(needle);
            return pos == -1 ? strVal : strVal.Slice(0, pos);
        }

        public static string HandlerFactoryPath;

        private string pathInfo;
        public string PathInfo
        {
            get
            {
                if (this.pathInfo == null)
                {
                    var mode = HandlerFactoryPath;

                    var pos = request.RawUrl.IndexOf('?', StringComparison.Ordinal);
                    if (pos != -1)
                    {
                        var path = request.RawUrl.Substring(0, pos);
                        this.pathInfo = GetPathInfo(
                            path,
                            mode,
                            mode ?? string.Empty);
                    }
                    else
                    {
                        this.pathInfo = request.RawUrl;
                    }

                    this.pathInfo = System.Net.WebUtility.UrlDecode(pathInfo);
                    this.pathInfo = NormalizePathInfo(pathInfo, mode);
                }
                return this.pathInfo;
            }
        }

        private static string GetPathInfo(string fullPath, string mode, string appPath)
        {
            var pathInfo = ResolvePathInfoFromMappedPath(fullPath, mode);
            if (!string.IsNullOrEmpty(pathInfo))
            {
                return pathInfo;
            }

            // Wildcard mode relies on this to work out the handlerPath
            pathInfo = ResolvePathInfoFromMappedPath(fullPath, appPath);
            if (!string.IsNullOrEmpty(pathInfo))
            {
                return pathInfo;
            }

            return fullPath;
        }

        private static string ResolvePathInfoFromMappedPath(string fullPath, string mappedPathRoot)
        {
            if (mappedPathRoot == null)
            {
                return null;
            }

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

            if (!pathRootFound)
            {
                return null;
            }

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
                    foreach (var cookie in this.request.Cookies)
                    {
                        var httpCookie = (System.Net.Cookie)cookie;
                        cookies[httpCookie.Name] = new System.Net.Cookie(httpCookie.Name, httpCookie.Value, httpCookie.Path, httpCookie.Domain);
                    }
                }

                return cookies;
            }
        }

        public string UserAgent => request.UserAgent;

        public QueryParamCollection Headers => request.Headers;

        private QueryParamCollection queryString;
        public QueryParamCollection QueryString => queryString ?? (queryString = MyHttpUtility.ParseQueryString(request.Url.Query));

        public bool IsLocal => request.IsLocal;

        private string httpMethod;
        public string HttpMethod =>
            httpMethod
            ?? (httpMethod = request.HttpMethod);

        public string Verb => HttpMethod;

        public string ContentType => request.ContentType;

        public Encoding contentEncoding;
        public Encoding ContentEncoding
        {
            get => contentEncoding ?? request.ContentEncoding;
            set => contentEncoding = value;
        }

        public Uri UrlReferrer => request.UrlReferrer;

        public static Encoding GetEncoding(string contentTypeHeader)
        {
            var param = GetParameter(contentTypeHeader, "charset=");
            if (param == null)
            {
                return null;
            }

            try
            {
                return Encoding.GetEncoding(param);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public Stream InputStream => request.InputStream;

        public long ContentLength => request.ContentLength64;

        private IHttpFile[] httpFiles;
        public IHttpFile[] Files
        {
            get
            {
                if (httpFiles == null)
                {
                    if (files == null)
                    {
                        return httpFiles = Array.Empty<IHttpFile>();
                    }

                    httpFiles = new IHttpFile[files.Count];
                    var i = 0;
                    foreach (var pair in files)
                    {
                        var reqFile = pair.Value;
                        httpFiles[i] = new HttpFile
                        {
                            ContentType = reqFile.ContentType,
                            ContentLength = reqFile.ContentLength,
                            FileName = reqFile.FileName,
                            InputStream = reqFile.InputStream,
                        };
                        i++;
                    }
                }
                return httpFiles;
            }
        }

        public static string NormalizePathInfo(string pathInfo, string handlerPath)
        {
            if (handlerPath != null)
            {
                var trimmed = pathInfo.TrimStart('/');
                if (trimmed.StartsWith(handlerPath, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring(handlerPath.Length);
                }
            }

            return pathInfo;
        }
    }
}
