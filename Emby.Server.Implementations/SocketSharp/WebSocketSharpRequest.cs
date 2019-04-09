using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using IHttpFile = MediaBrowser.Model.Services.IHttpFile;
using IHttpRequest = MediaBrowser.Model.Services.IHttpRequest;
using IResponse = MediaBrowser.Model.Services.IResponse;

namespace Emby.Server.Implementations.SocketSharp
{
    public partial class WebSocketSharpRequest : IHttpRequest
    {
        private readonly HttpRequest request;

        public WebSocketSharpRequest(HttpRequest httpContext, HttpResponse response, string operationName, ILogger logger)
        {
            this.OperationName = operationName;
            this.request = httpContext;
            this.Response = new WebSocketSharpResponse(logger, response);
        }

        public HttpRequest HttpRequest => request;

        public IResponse Response { get; }

        public string OperationName { get; set; }

        public object Dto { get; set; }

        public string RawUrl => request.GetEncodedPathAndQuery();

        public string AbsoluteUri => request.GetDisplayUrl().TrimEnd('/');

        public string XForwardedFor
            => StringValues.IsNullOrEmpty(request.Headers["X-Forwarded-For"]) ? null : request.Headers["X-Forwarded-For"].ToString();

        public int? XForwardedPort
            => StringValues.IsNullOrEmpty(request.Headers["X-Forwarded-Port"]) ? (int?)null : int.Parse(request.Headers["X-Forwarded-Port"], CultureInfo.InvariantCulture);

        public string XForwardedProtocol => StringValues.IsNullOrEmpty(request.Headers["X-Forwarded-Proto"]) ? null : request.Headers["X-Forwarded-Proto"].ToString();

        public string XRealIp => StringValues.IsNullOrEmpty(request.Headers["X-Real-IP"]) ? null : request.Headers["X-Real-IP"].ToString();

        private string remoteIp;
        public string RemoteIp
        {
            get
            {
                if (remoteIp != null)
                {
                    return remoteIp;
                }

                var temp = CheckBadChars(XForwardedFor.AsSpan());
                if (temp.Length != 0)
                {
                    return remoteIp = temp.ToString();
                }

                temp = CheckBadChars(XRealIp.AsSpan());
                if (temp.Length != 0)
                {
                    return remoteIp = NormalizeIp(temp).ToString();
                }

                return remoteIp = NormalizeIp(request.HttpContext.Connection.RemoteIpAddress.ToString().AsSpan()).ToString();
            }
        }

        private static readonly char[] HttpTrimCharacters = new char[] { (char)0x09, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20 };

        // CheckBadChars - throws on invalid chars to be not found in header name/value
        internal static ReadOnlySpan<char> CheckBadChars(ReadOnlySpan<char> name)
        {
            if (name.Length == 0)
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
                            throw new ArgumentException("net_WebHeaderInvalidControlChars", nameof(name));
                        }

                        break;

                    case 1:
                        if (c == '\n')
                        {
                            crlf = 2;
                            break;
                        }

                        throw new ArgumentException("net_WebHeaderInvalidCRLFChars", nameof(name));

                    case 2:
                        if (c == ' ' || c == '\t')
                        {
                            crlf = 0;
                            break;
                        }

                        throw new ArgumentException("net_WebHeaderInvalidCRLFChars", nameof(name));
                }
            }

            if (crlf != 0)
            {
                throw new ArgumentException("net_WebHeaderInvalidCRLFChars", nameof(name));
            }

            return name;
        }

        private ReadOnlySpan<char> NormalizeIp(ReadOnlySpan<char> ip)
        {
            if (ip.Length != 0 && !ip.IsWhiteSpace())
            {
                // Handle ipv4 mapped to ipv6
                const string srch = "::ffff:";
                var index = ip.IndexOf(srch.AsSpan(), StringComparison.OrdinalIgnoreCase);
                if (index == 0)
                {
                    ip = ip.Slice(srch.Length);
                }
            }

            return ip;
        }

        public string[] AcceptTypes => request.Headers.GetCommaSeparatedValues(HeaderNames.Accept);

        private Dictionary<string, object> items;
        public Dictionary<string, object> Items => items ?? (items = new Dictionary<string, object>());

        private string responseContentType;
        public string ResponseContentType
        {
            get =>
                responseContentType
                ?? (responseContentType = GetResponseContentType(HttpRequest));
            set => this.responseContentType = value;
        }

        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        public const string MultiPartFormData = "multipart/form-data";
        public static string GetResponseContentType(HttpRequest httpReq)
        {
            var specifiedContentType = GetQueryStringContentType(httpReq);
            if (!string.IsNullOrEmpty(specifiedContentType))
            {
                return specifiedContentType;
            }

            const string serverDefaultContentType = "application/json";

            var acceptContentTypes = httpReq.Headers.GetCommaSeparatedValues(HeaderNames.Accept);
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
                    var contentType = acceptsType?.Split(';')[0].Trim();
                    acceptsAnything = contentType.Equals("*/*", StringComparison.OrdinalIgnoreCase);

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

        public static bool HasAnyOfContentTypes(HttpRequest request, params string[] contentTypes)
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

        public static bool IsContentType(HttpRequest request, string contentType)
        {
            return request.ContentType.StartsWith(contentType, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetQueryStringContentType(HttpRequest httpReq)
        {
            ReadOnlySpan<char> format = httpReq.Query["format"].ToString().AsSpan();
            if (format == null)
            {
                const int formatMaxLength = 4;
                ReadOnlySpan<char> pi = httpReq.Path.ToString().AsSpan();
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
            if (format.Contains("json".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return "application/json";
            }
            else if (format.Contains("xml".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return "application/xml";
            }

            return null;
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

        public string PathInfo => this.request.Path.Value;

        public string UserAgent => request.Headers[HeaderNames.UserAgent];

        public IHeaderDictionary Headers => request.Headers;

        public IQueryCollection QueryString => request.Query;

        public bool IsLocal => string.Equals(request.HttpContext.Connection.LocalIpAddress.ToString(), request.HttpContext.Connection.RemoteIpAddress.ToString());

        private string httpMethod;
        public string HttpMethod =>
            httpMethod
            ?? (httpMethod = request.Method);

        public string Verb => HttpMethod;

        public string ContentType => request.ContentType;

        private Encoding ContentEncoding
        {
            get
            {
                // TODO is this necessary?
                if (UserAgent != null && CultureInfo.InvariantCulture.CompareInfo.IsPrefix(UserAgent, "UP"))
                {
                    string postDataCharset = Headers["x-up-devcap-post-charset"];
                    if (!string.IsNullOrEmpty(postDataCharset))
                    {
                        try
                        {
                            return Encoding.GetEncoding(postDataCharset);
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }

                return request.GetTypedHeaders().ContentType.Encoding ?? Encoding.UTF8;
            }
        }

        public Uri UrlReferrer => request.GetTypedHeaders().Referer;

        public static Encoding GetEncoding(string contentTypeHeader)
        {
            var param = GetParameter(contentTypeHeader.AsSpan(), "charset=");
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

        public Stream InputStream => request.Body;

        public long ContentLength => request.ContentLength ?? 0;

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
    }
}
