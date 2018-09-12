using System;
using System.Text;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Text;

namespace SocketHttpListener.Net
{
    public sealed partial class HttpListenerRequest
    {
        private long _contentLength;
        private bool _clSet;
        private WebHeaderCollection _headers;
        private string _method;
        private Stream _inputStream;
        private HttpListenerContext _context;
        private bool _isChunked;

        private static byte[] s_100continue = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;
            _headers = new WebHeaderCollection();
            _version = HttpVersion.Version10;
        }

        private static readonly char[] s_separators = new char[] { ' ' };

        internal void SetRequestLine(string req)
        {
            string[] parts = req.Split(s_separators, 3);
            if (parts.Length != 3)
            {
                _context.ErrorMessage = "Invalid request line (parts).";
                return;
            }

            _method = parts[0];
            foreach (char c in _method)
            {
                int ic = (int)c;

                if ((ic >= 'A' && ic <= 'Z') ||
                    (ic > 32 && c < 127 && c != '(' && c != ')' && c != '<' &&
                     c != '<' && c != '>' && c != '@' && c != ',' && c != ';' &&
                     c != ':' && c != '\\' && c != '"' && c != '/' && c != '[' &&
                     c != ']' && c != '?' && c != '=' && c != '{' && c != '}'))
                    continue;

                _context.ErrorMessage = "(Invalid verb)";
                return;
            }

            _rawUrl = parts[1];
            if (parts[2].Length != 8 || !parts[2].StartsWith("HTTP/"))
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }

            try
            {
                _version = new Version(parts[2].Substring(5));
            }
            catch
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }

            if (_version.Major < 1)
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }
            if (_version.Major > 1)
            {
                _context.ErrorStatus = (int)HttpStatusCode.HttpVersionNotSupported;
                _context.ErrorMessage = HttpStatusDescription.Get(HttpStatusCode.HttpVersionNotSupported);
                return;
            }
        }

        private static bool MaybeUri(string s)
        {
            int p = s.IndexOf(':');
            if (p == -1)
                return false;

            if (p >= 10)
                return false;

            return IsPredefinedScheme(s.Substring(0, p));
        }

        private static bool IsPredefinedScheme(string scheme)
        {
            if (scheme == null || scheme.Length < 3)
                return false;

            char c = scheme[0];
            if (c == 'h')
                return (scheme == UriScheme.Http || scheme == UriScheme.Https);
            if (c == 'f')
                return (scheme == UriScheme.File || scheme == UriScheme.Ftp);

            if (c == 'n')
            {
                c = scheme[1];
                if (c == 'e')
                    return (scheme == UriScheme.News || scheme == UriScheme.NetPipe || scheme == UriScheme.NetTcp);
                if (scheme == UriScheme.Nntp)
                    return true;
                return false;
            }
            if ((c == 'g' && scheme == UriScheme.Gopher) || (c == 'm' && scheme == UriScheme.Mailto))
                return true;

            return false;
        }

        internal void FinishInitialization()
        {
            string host = UserHostName;
            if (_version > HttpVersion.Version10 && (host == null || host.Length == 0))
            {
                _context.ErrorMessage = "Invalid host name";
                return;
            }

            string path;
            Uri raw_uri = null;
            if (MaybeUri(_rawUrl.ToLowerInvariant()) && Uri.TryCreate(_rawUrl, UriKind.Absolute, out raw_uri))
                path = raw_uri.PathAndQuery;
            else
                path = _rawUrl;

            if ((host == null || host.Length == 0))
                host = UserHostAddress;

            if (raw_uri != null)
                host = raw_uri.Host;

            int colon = host.IndexOf(']') == -1 ? host.IndexOf(':') : host.LastIndexOf(':');
            if (colon >= 0)
                host = host.Substring(0, colon);

            string base_uri = string.Format("{0}://{1}:{2}", RequestScheme, host, LocalEndPoint.Port);

            if (!Uri.TryCreate(base_uri + path, UriKind.Absolute, out _requestUri))
            {
                _context.ErrorMessage = System.Net.WebUtility.HtmlEncode("Invalid url: " + base_uri + path);
                return;
            }

            _requestUri = HttpListenerRequestUriBuilder.GetRequestUri(_rawUrl, _requestUri.Scheme,
                                _requestUri.Authority, _requestUri.LocalPath, _requestUri.Query);

            if (_version >= HttpVersion.Version11)
            {
                string t_encoding = Headers[HttpKnownHeaderNames.TransferEncoding];
                _isChunked = (t_encoding != null && string.Equals(t_encoding, "chunked", StringComparison.OrdinalIgnoreCase));
                // 'identity' is not valid!
                if (t_encoding != null && !_isChunked)
                {
                    _context.Connection.SendError(null, 501);
                    return;
                }
            }

            if (!_isChunked && !_clSet)
            {
                if (string.Equals(_method, "POST", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_method, "PUT", StringComparison.OrdinalIgnoreCase))
                {
                    _context.Connection.SendError(null, 411);
                    return;
                }
            }

            if (String.Compare(Headers[HttpKnownHeaderNames.Expect], "100-continue", StringComparison.OrdinalIgnoreCase) == 0)
            {
                HttpResponseStream output = _context.Connection.GetResponseStream();
                output.InternalWrite(s_100continue, 0, s_100continue.Length);
            }
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
                _context.ErrorMessage = HttpStatusDescription.Get(400);
                _context.ErrorStatus = 400;
                return;
            }

            string name = header.Substring(0, colon).Trim();
            string val = header.Substring(colon + 1).Trim();
            if (name.Equals("content-length", StringComparison.OrdinalIgnoreCase))
            {
                // To match Windows behavior:
                // Content lengths >= 0 and <= long.MaxValue are accepted as is.
                // Content lengths > long.MaxValue and <= ulong.MaxValue are treated as 0.
                // Content lengths < 0 cause the requests to fail.
                // Other input is a failure, too.
                long parsedContentLength =
                    ulong.TryParse(val, out ulong parsedUlongContentLength) ? (parsedUlongContentLength <= long.MaxValue ? (long)parsedUlongContentLength : 0) :
                    long.Parse(val);
                if (parsedContentLength < 0 || (_clSet && parsedContentLength != _contentLength))
                {
                    _context.ErrorMessage = "Invalid Content-Length.";
                }
                else
                {
                    _contentLength = parsedContentLength;
                    _clSet = true;
                }
            }
            else if (name.Equals("transfer-encoding", StringComparison.OrdinalIgnoreCase))
            {
                if (Headers[HttpKnownHeaderNames.TransferEncoding] != null)
                {
                    _context.ErrorStatus = (int)HttpStatusCode.NotImplemented;
                    _context.ErrorMessage = HttpStatusDescription.Get(HttpStatusCode.NotImplemented);
                }
            }

            if (_context.ErrorMessage == null)
            {
                _headers.Set(name, val);
            }
        }

        // returns true is the stream could be reused.
        internal bool FlushInput()
        {
            if (!HasEntityBody)
                return true;

            int length = 2048;
            if (_contentLength > 0)
                length = (int)Math.Min(_contentLength, (long)length);

            byte[] bytes = new byte[length];
            while (true)
            {
                try
                {
                    IAsyncResult ares = InputStream.BeginRead(bytes, 0, length, null, null);
                    if (!ares.IsCompleted && !ares.AsyncWaitHandle.WaitOne(1000))
                        return false;
                    if (InputStream.EndRead(ares) <= 0)
                        return true;
                }
                catch (ObjectDisposedException)
                {
                    _inputStream = null;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public long ContentLength64
        {
            get
            {
                if (_isChunked)
                    _contentLength = -1;

                return _contentLength;
            }
        }

        public bool HasEntityBody => (_contentLength > 0 || _isChunked);

        public QueryParamCollection Headers => _headers;

        public string HttpMethod => _method;

        public Stream InputStream
        {
            get
            {
                if (_inputStream == null)
                {
                    if (_isChunked || _contentLength > 0)
                        _inputStream = _context.Connection.GetRequestStream(_isChunked, _contentLength);
                    else
                        _inputStream = Stream.Null;
                }

                return _inputStream;
            }
        }

        public bool IsAuthenticated => false;

        public bool IsSecureConnection => _context.Connection.IsSecure;

        public System.Net.IPEndPoint LocalEndPoint => _context.Connection.LocalEndPoint;

        public System.Net.IPEndPoint RemoteEndPoint => _context.Connection.RemoteEndPoint;

        public Guid RequestTraceIdentifier { get; } = Guid.NewGuid();

        public string ServiceName => null;

        private Uri RequestUri => _requestUri;
        private bool SupportsWebSockets => true;
    }
}
