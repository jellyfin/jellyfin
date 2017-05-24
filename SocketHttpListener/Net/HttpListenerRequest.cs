using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    public sealed class HttpListenerRequest
    {
        string[] accept_types;
        Encoding content_encoding;
        long content_length;
        bool cl_set;
        CookieCollection cookies;
        WebHeaderCollection headers;
        string method;
        Stream input_stream;
        Version version;
        QueryParamCollection query_string; // check if null is ok, check if read-only, check case-sensitiveness
        string raw_url;
        Uri url;
        Uri referrer;
        string[] user_languages;
        HttpListenerContext context;
        bool is_chunked;
        bool ka_set;
        bool keep_alive;

        private readonly ITextEncoding _textEncoding;

        internal HttpListenerRequest(HttpListenerContext context, ITextEncoding textEncoding)
        {
            this.context = context;
            _textEncoding = textEncoding;
            headers = new WebHeaderCollection();
            version = HttpVersion.Version10;
        }

        static char[] separators = new char[] { ' ' };

        internal void SetRequestLine(string req)
        {
            string[] parts = req.Split(separators, 3);
            if (parts.Length != 3)
            {
                context.ErrorMessage = "Invalid request line (parts).";
                return;
            }

            method = parts[0];
            foreach (char c in method)
            {
                int ic = (int)c;

                if ((ic >= 'A' && ic <= 'Z') ||
                    (ic > 32 && c < 127 && c != '(' && c != ')' && c != '<' &&
                     c != '<' && c != '>' && c != '@' && c != ',' && c != ';' &&
                     c != ':' && c != '\\' && c != '"' && c != '/' && c != '[' &&
                     c != ']' && c != '?' && c != '=' && c != '{' && c != '}'))
                    continue;

                context.ErrorMessage = "(Invalid verb)";
                return;
            }

            raw_url = parts[1];
            if (parts[2].Length != 8 || !parts[2].StartsWith("HTTP/"))
            {
                context.ErrorMessage = "Invalid request line (version).";
                return;
            }

            try
            {
                version = new Version(parts[2].Substring(5));
                if (version.Major < 1)
                    throw new Exception();
            }
            catch
            {
                context.ErrorMessage = "Invalid request line (version).";
                return;
            }
        }

        void CreateQueryString(string query)
        {
            if (query == null || query.Length == 0)
            {
                query_string = new QueryParamCollection();
                return;
            }

            query_string = new QueryParamCollection();
            if (query[0] == '?')
                query = query.Substring(1);
            string[] components = query.Split('&');
            foreach (string kv in components)
            {
                int pos = kv.IndexOf('=');
                if (pos == -1)
                {
                    query_string.Add(null, WebUtility.UrlDecode(kv));
                }
                else
                {
                    string key = WebUtility.UrlDecode(kv.Substring(0, pos));
                    string val = WebUtility.UrlDecode(kv.Substring(pos + 1));

                    query_string.Add(key, val);
                }
            }
        }

        internal void FinishInitialization()
        {
            string host = UserHostName;
            if (version > HttpVersion.Version10 && (host == null || host.Length == 0))
            {
                context.ErrorMessage = "Invalid host name";
                return;
            }

            string path;
            Uri raw_uri = null;
            if (MaybeUri(raw_url.ToLowerInvariant()) && Uri.TryCreate(raw_url, UriKind.Absolute, out raw_uri))
                path = raw_uri.PathAndQuery;
            else
                path = raw_url;

            if ((host == null || host.Length == 0))
                host = UserHostAddress;

            if (raw_uri != null)
                host = raw_uri.Host;

            int colon = host.LastIndexOf(':');
            if (colon >= 0)
                host = host.Substring(0, colon);

            string base_uri = String.Format("{0}://{1}:{2}",
                (IsSecureConnection) ? (IsWebSocketRequest ? "wss" : "https") : (IsWebSocketRequest ? "ws" : "http"),
                                host, LocalEndPoint.Port);

            if (!Uri.TryCreate(base_uri + path, UriKind.Absolute, out url))
            {
                context.ErrorMessage = WebUtility.HtmlEncode("Invalid url: " + base_uri + path);
                return; return;
            }

            CreateQueryString(url.Query);

            if (version >= HttpVersion.Version11)
            {
                string t_encoding = Headers["Transfer-Encoding"];
                is_chunked = (t_encoding != null && String.Compare(t_encoding, "chunked", StringComparison.OrdinalIgnoreCase) == 0);
                // 'identity' is not valid!
                if (t_encoding != null && !is_chunked)
                {
                    context.Connection.SendError(null, 501);
                    return;
                }
            }

            if (!is_chunked && !cl_set)
            {
                if (String.Compare(method, "POST", StringComparison.OrdinalIgnoreCase) == 0 ||
                    String.Compare(method, "PUT", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    context.Connection.SendError(null, 411);
                    return;
                }
            }

            if (String.Compare(Headers["Expect"], "100-continue", StringComparison.OrdinalIgnoreCase) == 0)
            {
                var output = (HttpResponseStream)context.Connection.GetResponseStream(true);

                var _100continue = _textEncoding.GetASCIIEncoding().GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

                output.InternalWrite(_100continue, 0, _100continue.Length);
            }
        }

        static bool MaybeUri(string s)
        {
            int p = s.IndexOf(':');
            if (p == -1)
                return false;

            if (p >= 10)
                return false;

            return IsPredefinedScheme(s.Substring(0, p));
        }

        //
        // Using a simple block of if's is twice as slow as the compiler generated
        // switch statement.   But using this tuned code is faster than the
        // compiler generated code, with a million loops on x86-64:
        //
        // With "http": .10 vs .51 (first check)
        // with "https": .16 vs .51 (second check)
        // with "foo": .22 vs .31 (never found)
        // with "mailto": .12 vs .51  (last check)
        //
        //
        static bool IsPredefinedScheme(string scheme)
        {
            if (scheme == null || scheme.Length < 3)
                return false;

            char c = scheme[0];
            if (c == 'h')
                return (scheme == "http" || scheme == "https");
            if (c == 'f')
                return (scheme == "file" || scheme == "ftp");

            if (c == 'n')
            {
                c = scheme[1];
                if (c == 'e')
                    return (scheme == "news" || scheme == "net.pipe" || scheme == "net.tcp");
                if (scheme == "nntp")
                    return true;
                return false;
            }
            if ((c == 'g' && scheme == "gopher") || (c == 'm' && scheme == "mailto"))
                return true;

            return false;
        }

        internal static string Unquote(String str)
        {
            int start = str.IndexOf('\"');
            int end = str.LastIndexOf('\"');
            if (start >= 0 && end >= 0)
                str = str.Substring(start + 1, end - 1);
            return str.Trim();
        }

        internal void AddHeader(string header)
        {
            int colon = header.IndexOf(':');
            if (colon == -1 || colon == 0)
            {
                context.ErrorMessage = "Bad Request";
                context.ErrorStatus = 400;
                return;
            }

            string name = header.Substring(0, colon).Trim();
            string val = header.Substring(colon + 1).Trim();
            string lower = name.ToLowerInvariant();
            headers.SetInternal(name, val);
            switch (lower)
            {
                case "accept-language":
                    user_languages = val.Split(','); // yes, only split with a ','
                    break;
                case "accept":
                    accept_types = val.Split(','); // yes, only split with a ','
                    break;
                case "content-length":
                    try
                    {
                        //TODO: max. content_length?
                        content_length = Int64.Parse(val.Trim());
                        if (content_length < 0)
                            context.ErrorMessage = "Invalid Content-Length.";
                        cl_set = true;
                    }
                    catch
                    {
                        context.ErrorMessage = "Invalid Content-Length.";
                    }

                    break;
                case "content-type":
                    {
                        var contents = val.Split(';');
                        foreach (var content in contents)
                        {
                            var tmp = content.Trim();
                            if (tmp.StartsWith("charset"))
                            {
                                var charset = tmp.GetValue("=");
                                if (charset != null && charset.Length > 0)
                                {
                                    try
                                    {

                                        // Support upnp/dlna devices - CONTENT-TYPE: text/xml ; charset="utf-8"\r\n
                                        charset = charset.Trim('"');
                                        var index = charset.IndexOf('"');
                                        if (index != -1) charset = charset.Substring(0, index);

                                        content_encoding = Encoding.GetEncoding(charset);
                                    }
                                    catch
                                    {
                                        context.ErrorMessage = "Invalid Content-Type header: " + charset;
                                    }
                                }

                                break;
                            }
                        }
                    }
                    break;
                case "referer":
                    try
                    {
                        referrer = new Uri(val);
                    }
                    catch
                    {
                        referrer = new Uri("http://someone.is.screwing.with.the.headers.com/");
                    }
                    break;
                case "cookie":
                    if (cookies == null)
                        cookies = new CookieCollection();

                    string[] cookieStrings = val.Split(new char[] { ',', ';' });
                    Cookie current = null;
                    int version = 0;
                    foreach (string cookieString in cookieStrings)
                    {
                        string str = cookieString.Trim();
                        if (str.Length == 0)
                            continue;
                        if (str.StartsWith("$Version"))
                        {
                            version = Int32.Parse(Unquote(str.Substring(str.IndexOf('=') + 1)));
                        }
                        else if (str.StartsWith("$Path"))
                        {
                            if (current != null)
                                current.Path = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else if (str.StartsWith("$Domain"))
                        {
                            if (current != null)
                                current.Domain = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else if (str.StartsWith("$Port"))
                        {
                            if (current != null)
                                current.Port = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else
                        {
                            if (current != null)
                            {
                                cookies.Add(current);
                            }
                            current = new Cookie();
                            int idx = str.IndexOf('=');
                            if (idx > 0)
                            {
                                current.Name = str.Substring(0, idx).Trim();
                                current.Value = str.Substring(idx + 1).Trim();
                            }
                            else
                            {
                                current.Name = str.Trim();
                                current.Value = String.Empty;
                            }
                            current.Version = version;
                        }
                    }
                    if (current != null)
                    {
                        cookies.Add(current);
                    }
                    break;
            }
        }

        // returns true is the stream could be reused.
        internal bool FlushInput()
        {
            if (!HasEntityBody)
                return true;

            int length = 2048;
            if (content_length > 0)
                length = (int)Math.Min(content_length, (long)length);

            byte[] bytes = new byte[length];
            while (true)
            {
                // TODO: test if MS has a timeout when doing this
                try
                {
                    var task = InputStream.ReadAsync(bytes, 0, length);
                    var result = Task.WaitAll(new [] { task }, 1000);
                    if (!result)
                    {
                        return false;
                    }
                    if (task.Result <= 0)
                    {
                        return true;
                    }
                }
                catch (ObjectDisposedException e)
                {
                    input_stream = null;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public string[] AcceptTypes
        {
            get { return accept_types; }
        }

        public int ClientCertificateError
        {
            get
            {
                HttpConnection cnc = context.Connection;
                //if (cnc.ClientCertificate == null)
                //    throw new InvalidOperationException("No client certificate");
                //int[] errors = cnc.ClientCertificateErrors;
                //if (errors != null && errors.Length > 0)
                //    return errors[0];
                return 0;
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                if (content_encoding == null)
                    content_encoding = _textEncoding.GetDefaultEncoding();
                return content_encoding;
            }
        }

        public long ContentLength64
        {
            get { return is_chunked ? -1 : content_length; }
        }

        public string ContentType
        {
            get { return headers["content-type"]; }
        }

        public CookieCollection Cookies
        {
            get
            {
                // TODO: check if the collection is read-only
                if (cookies == null)
                    cookies = new CookieCollection();
                return cookies;
            }
        }

        public bool HasEntityBody
        {
            get { return (content_length > 0 || is_chunked); }
        }

        public QueryParamCollection Headers
        {
            get { return headers; }
        }

        public string HttpMethod
        {
            get { return method; }
        }

        public Stream InputStream
        {
            get
            {
                if (input_stream == null)
                {
                    if (is_chunked || content_length > 0)
                        input_stream = context.Connection.GetRequestStream(is_chunked, content_length);
                    else
                        input_stream = Stream.Null;
                }

                return input_stream;
            }
        }

        public bool IsAuthenticated
        {
            get { return false; }
        }

        public bool IsLocal
        {
            get { return RemoteEndPoint.IpAddress.Equals(IpAddressInfo.Loopback) || RemoteEndPoint.IpAddress.Equals(IpAddressInfo.IPv6Loopback) || LocalEndPoint.IpAddress.Equals(RemoteEndPoint.IpAddress); }
        }

        public bool IsSecureConnection
        {
            get { return context.Connection.IsSecure; }
        }

        public bool KeepAlive
        {
            get
            {
                if (ka_set)
                    return keep_alive;

                ka_set = true;
                // 1. Connection header
                // 2. Protocol (1.1 == keep-alive by default)
                // 3. Keep-Alive header
                string cnc = headers["Connection"];
                if (!String.IsNullOrEmpty(cnc))
                {
                    keep_alive = (0 == String.Compare(cnc, "keep-alive", StringComparison.OrdinalIgnoreCase));
                }
                else if (version == HttpVersion.Version11)
                {
                    keep_alive = true;
                }
                else
                {
                    cnc = headers["keep-alive"];
                    if (!String.IsNullOrEmpty(cnc))
                        keep_alive = (0 != String.Compare(cnc, "closed", StringComparison.OrdinalIgnoreCase));
                }
                return keep_alive;
            }
        }

        public IpEndPointInfo LocalEndPoint
        {
            get { return context.Connection.LocalEndPoint; }
        }

        public Version ProtocolVersion
        {
            get { return version; }
        }

        public QueryParamCollection QueryString
        {
            get { return query_string; }
        }

        public string RawUrl
        {
            get { return raw_url; }
        }

        public IpEndPointInfo RemoteEndPoint
        {
            get { return context.Connection.RemoteEndPoint; }
        }

        public Guid RequestTraceIdentifier
        {
            get { return Guid.Empty; }
        }

        public Uri Url
        {
            get { return url; }
        }

        public Uri UrlReferrer
        {
            get { return referrer; }
        }

        public string UserAgent
        {
            get { return headers["user-agent"]; }
        }

        public string UserHostAddress
        {
            get { return LocalEndPoint.ToString(); }
        }

        public string UserHostName
        {
            get { return headers["host"]; }
        }

        public string[] UserLanguages
        {
            get { return user_languages; }
        }

        public string ServiceName
        {
            get
            {
                return null;
            }
        }

        private bool _websocketRequestWasSet;
        private bool _websocketRequest;

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket connection request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket connection request; otherwise, <c>false</c>.
        /// </value>
        public bool IsWebSocketRequest
        {
            get
            {
                if (!_websocketRequestWasSet)
                {
                    _websocketRequest = method == "GET" &&
                                        version > HttpVersion.Version10 &&
                                        headers.Contains("Upgrade", "websocket") &&
                                        headers.Contains("Connection", "Upgrade");

                    _websocketRequestWasSet = true;
                }

                return _websocketRequest;
            }
        }

        public Task<ICertificate> GetClientCertificateAsync()
        {
            return Task.FromResult<ICertificate>(null);
        }
    }
}
