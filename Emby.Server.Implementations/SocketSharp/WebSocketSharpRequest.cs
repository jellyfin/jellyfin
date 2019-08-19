using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using IHttpRequest = MediaBrowser.Model.Services.IHttpRequest;

namespace Emby.Server.Implementations.SocketSharp
{
    public partial class WebSocketSharpRequest : IHttpRequest
    {
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        public const string MultiPartFormData = "multipart/form-data";
        public const string Soap11 = "text/xml; charset=utf-8";

        private string _remoteIp;
        private Dictionary<string, object> _items;
        private string _responseContentType;

        public WebSocketSharpRequest(HttpRequest httpRequest, HttpResponse httpResponse, string operationName, ILogger logger)
        {
            this.OperationName = operationName;
            this.Request = httpRequest;
            this.Response = httpResponse;
        }

        public string Accept => StringValues.IsNullOrEmpty(Request.Headers[HeaderNames.Accept]) ? null : Request.Headers[HeaderNames.Accept].ToString();

        public string Authorization => StringValues.IsNullOrEmpty(Request.Headers[HeaderNames.Authorization]) ? null : Request.Headers[HeaderNames.Authorization].ToString();

        public HttpRequest Request { get; }

        public HttpResponse Response { get; }

        public string OperationName { get; set; }

        public string RawUrl => Request.GetEncodedPathAndQuery();

        public string AbsoluteUri => Request.GetDisplayUrl().TrimEnd('/');

        public string RemoteIp
        {
            get
            {
                if (_remoteIp != null)
                {
                    return _remoteIp;
                }

                IPAddress ip;

                // "Real" remote ip might be in X-Forwarded-For of X-Real-Ip
                // (if the server is behind a reverse proxy for example)
                if (!IPAddress.TryParse(GetHeader(CustomHeaderNames.XForwardedFor), out ip))
                {
                    if (!IPAddress.TryParse(GetHeader(CustomHeaderNames.XRealIP), out ip))
                    {
                        ip = Request.HttpContext.Connection.RemoteIpAddress;
                    }
                }

                return _remoteIp = NormalizeIp(ip).ToString();
            }
        }

        public string[] AcceptTypes => Request.Headers.GetCommaSeparatedValues(HeaderNames.Accept);

        public Dictionary<string, object> Items => _items ?? (_items = new Dictionary<string, object>());

        public string ResponseContentType
        {
            get =>
                _responseContentType
                ?? (_responseContentType = GetResponseContentType(Request));
            set => this._responseContentType = value;
        }

        public string PathInfo => Request.Path.Value;

        public string UserAgent => Request.Headers[HeaderNames.UserAgent];

        public IHeaderDictionary Headers => Request.Headers;

        public IQueryCollection QueryString => Request.Query;

        public bool IsLocal => Request.HttpContext.Connection.LocalIpAddress.Equals(Request.HttpContext.Connection.RemoteIpAddress);


        public string HttpMethod => Request.Method;

        public string Verb => HttpMethod;

        public string ContentType => Request.ContentType;

        public Uri UrlReferrer => Request.GetTypedHeaders().Referer;

        public Stream InputStream => Request.Body;

        public long ContentLength => Request.ContentLength ?? 0;

        private string GetHeader(string name) => Request.Headers[name].ToString();

        private static IPAddress NormalizeIp(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
            {
                return ip.MapToIPv4();
            }

            return ip;
        }

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
    }
}
