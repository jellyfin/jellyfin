using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    public sealed class HttpListenerResponse : IDisposable
    {
        bool disposed;
        Encoding content_encoding;
        long content_length;
        bool cl_set;
        string content_type;
        CookieCollection cookies;
        WebHeaderCollection headers = new WebHeaderCollection();
        bool keep_alive = true;
        Stream output_stream;
        Version version = HttpVersion.Version11;
        string location;
        int status_code = 200;
        string status_description = "OK";
        bool chunked;
        HttpListenerContext context;

        internal bool HeadersSent;
        internal object headers_lock = new object();

        private readonly ILogger _logger;
        private readonly ITextEncoding _textEncoding;
        private readonly IFileSystem _fileSystem;

        internal HttpListenerResponse(HttpListenerContext context, ILogger logger, ITextEncoding textEncoding, IFileSystem fileSystem)
        {
            this.context = context;
            _logger = logger;
            _textEncoding = textEncoding;
            _fileSystem = fileSystem;
        }

        internal bool CloseConnection
        {
            get
            {
                return headers["Connection"] == "close";
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
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                content_encoding = value;
            }
        }

        public long ContentLength64
        {
            get { return content_length; }
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException("Cannot be changed after headers are sent.");

                if (value < 0)
                    throw new ArgumentOutOfRangeException("Must be >= 0", "value");

                cl_set = true;
                content_length = value;
            }
        }

        public string ContentType
        {
            get { return content_type; }
            set
            {
                // TODO: is null ok?
                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                content_type = value;
            }
        }

        // RFC 2109, 2965 + the netscape specification at http://wp.netscape.com/newsref/std/cookie_spec.html
        public CookieCollection Cookies
        {
            get
            {
                if (cookies == null)
                    cookies = new CookieCollection();
                return cookies;
            }
            set { cookies = value; } // null allowed?
        }

        public WebHeaderCollection Headers
        {
            get { return headers; }
            set
            {
                /**
                 *	"If you attempt to set a Content-Length, Keep-Alive, Transfer-Encoding, or
                 *	WWW-Authenticate header using the Headers property, an exception will be
                 *	thrown. Use the KeepAlive or ContentLength64 properties to set these headers.
                 *	You cannot set the Transfer-Encoding or WWW-Authenticate headers manually."
                */
                // TODO: check if this is marked readonly after headers are sent.
                headers = value;
            }
        }

        public bool KeepAlive
        {
            get { return keep_alive; }
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                keep_alive = value;
            }
        }

        public Stream OutputStream
        {
            get
            {
                if (output_stream == null)
                    output_stream = context.Connection.GetResponseStream();
                return output_stream;
            }
        }

        public Version ProtocolVersion
        {
            get { return version; }
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                    throw new ArgumentException("Must be 1.0 or 1.1", "value");

                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                version = value;
            }
        }

        public string RedirectLocation
        {
            get { return location; }
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                location = value;
            }
        }

        public bool SendChunked
        {
            get { return chunked; }
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                chunked = value;
            }
        }

        public int StatusCode
        {
            get { return status_code; }
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (value < 100 || value > 999)
                    throw new ProtocolViolationException("StatusCode must be between 100 and 999.");
                status_code = value;
                status_description = GetStatusDescription(value);
            }
        }

        internal static string GetStatusDescription(int code)
        {
            switch (code)
            {
                case 100: return "Continue";
                case 101: return "Switching Protocols";
                case 102: return "Processing";
                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative Information";
                case 204: return "No Content";
                case 205: return "Reset Content";
                case 206: return "Partial Content";
                case 207: return "Multi-Status";
                case 300: return "Multiple Choices";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 303: return "See Other";
                case 304: return "Not Modified";
                case 305: return "Use Proxy";
                case 307: return "Temporary Redirect";
                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 402: return "Payment Required";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 406: return "Not Acceptable";
                case 407: return "Proxy Authentication Required";
                case 408: return "Request Timeout";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length Required";
                case 412: return "Precondition Failed";
                case 413: return "Request Entity Too Large";
                case 414: return "Request-Uri Too Long";
                case 415: return "Unsupported Media Type";
                case 416: return "Requested Range Not Satisfiable";
                case 417: return "Expectation Failed";
                case 422: return "Unprocessable Entity";
                case 423: return "Locked";
                case 424: return "Failed Dependency";
                case 500: return "Internal Server Error";
                case 501: return "Not Implemented";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                case 504: return "Gateway Timeout";
                case 505: return "Http Version Not Supported";
                case 507: return "Insufficient Storage";
            }
            return "";
        }

        public string StatusDescription
        {
            get { return status_description; }
            set
            {
                status_description = value;
            }
        }

        void IDisposable.Dispose()
        {
            Close(true); //TODO: Abort or Close?
        }

        public void Abort()
        {
            if (disposed)
                return;

            Close(true);
        }

        public void AddHeader(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (name == "")
                throw new ArgumentException("'name' cannot be empty", "name");

            //TODO: check for forbidden headers and invalid characters
            if (value.Length > 65535)
                throw new ArgumentOutOfRangeException("value");

            headers.Set(name, value);
        }

        public void AppendCookie(Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException("cookie");

            Cookies.Add(cookie);
        }

        public void AppendHeader(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (name == "")
                throw new ArgumentException("'name' cannot be empty", "name");

            if (value.Length > 65535)
                throw new ArgumentOutOfRangeException("value");

            headers.Add(name, value);
        }

        private void Close(bool force)
        {
            if (force)
            {
                _logger.Debug("HttpListenerResponse force closing HttpConnection");
            }
            disposed = true;
            context.Connection.Close(force);
        }

        public void Close()
        {
            if (disposed)
                return;

            Close(false);
        }

        public void Redirect(string url)
        {
            StatusCode = 302; // Found
            location = url;
        }

        bool FindCookie(Cookie cookie)
        {
            string name = cookie.Name;
            string domain = cookie.Domain;
            string path = cookie.Path;
            foreach (Cookie c in cookies)
            {
                if (name != c.Name)
                    continue;
                if (domain != c.Domain)
                    continue;
                if (path == c.Path)
                    return true;
            }

            return false;
        }

        public void DetermineIfChunked()
        {
            if (chunked)
            {
                return;
            }

            Version v = context.Request.ProtocolVersion;
            if (!cl_set && !chunked && v >= HttpVersion.Version11)
                chunked = true;
            if (!chunked && string.Equals(headers["Transfer-Encoding"], "chunked"))
            {
                chunked = true;
            }
        }

        internal void SendHeaders(bool closing, MemoryStream ms)
        {
            Encoding encoding = content_encoding;
            if (encoding == null)
                encoding = _textEncoding.GetDefaultEncoding();

            if (content_type != null)
            {
                if (content_encoding != null && content_type.IndexOf("charset=", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    string enc_name = content_encoding.WebName;
                    headers.SetInternal("Content-Type", content_type + "; charset=" + enc_name);
                }
                else
                {
                    headers.SetInternal("Content-Type", content_type);
                }
            }

            if (headers["Server"] == null)
                headers.SetInternal("Server", "Mono-HTTPAPI/1.0");

            CultureInfo inv = CultureInfo.InvariantCulture;
            if (headers["Date"] == null)
                headers.SetInternal("Date", DateTime.UtcNow.ToString("r", inv));

            if (!chunked)
            {
                if (!cl_set && closing)
                {
                    cl_set = true;
                    content_length = 0;
                }

                if (cl_set)
                    headers.SetInternal("Content-Length", content_length.ToString(inv));
            }

            Version v = context.Request.ProtocolVersion;
            if (!cl_set && !chunked && v >= HttpVersion.Version11)
                chunked = true;

            /* Apache forces closing the connection for these status codes:
             *	HttpStatusCode.BadRequest 		400
             *	HttpStatusCode.RequestTimeout 		408
             *	HttpStatusCode.LengthRequired 		411
             *	HttpStatusCode.RequestEntityTooLarge 	413
             *	HttpStatusCode.RequestUriTooLong 	414
             *	HttpStatusCode.InternalServerError 	500
             *	HttpStatusCode.ServiceUnavailable 	503
             */
            bool conn_close = status_code == 400 || status_code == 408 || status_code == 411 ||
                    status_code == 413 || status_code == 414 ||
                    status_code == 500 ||
                    status_code == 503;

            if (conn_close == false)
                conn_close = !context.Request.KeepAlive;

            // They sent both KeepAlive: true and Connection: close!?
            if (!keep_alive || conn_close)
            {
                headers.SetInternal("Connection", "close");
                conn_close = true;
            }

            if (chunked)
                headers.SetInternal("Transfer-Encoding", "chunked");

            //int reuses = context.Connection.Reuses;
            //if (reuses >= 100)
            //{
            //    _logger.Debug("HttpListenerResponse - keep alive has exceeded 100 uses and will be closed.");

            //    force_close_chunked = true;
            //    if (!conn_close)
            //    {
            //        headers.SetInternal("Connection", "close");
            //        conn_close = true;
            //    }
            //}

            if (!conn_close)
            {
                if (context.Request.ProtocolVersion <= HttpVersion.Version10)
                    headers.SetInternal("Connection", "keep-alive");
            }

            if (location != null)
                headers.SetInternal("Location", location);

            if (cookies != null)
            {
                foreach (Cookie cookie in cookies)
                    headers.SetInternal("Set-Cookie", cookie.ToString());
            }

            headers.SetInternal("Status", status_code.ToString(CultureInfo.InvariantCulture));

            using (StreamWriter writer = new StreamWriter(ms, encoding, 256, true))
            {
                writer.Write("HTTP/{0} {1} {2}\r\n", version, status_code, status_description);
                string headers_str = headers.ToStringMultiValue();
                writer.Write(headers_str);
                writer.Flush();
            }

            int preamble = encoding.GetPreamble().Length;
            if (output_stream == null)
                output_stream = context.Connection.GetResponseStream();

            /* Assumes that the ms was at position 0 */
            ms.Position = preamble;
            HeadersSent = true;
        }

        public void SetCookie(Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException("cookie");

            if (cookies != null)
            {
                if (FindCookie(cookie))
                    throw new ArgumentException("The cookie already exists.");
            }
            else
            {
                cookies = new CookieCollection();
            }

            cookies.Add(cookie);
        }

        public Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken)
        {
            return ((ResponseStream)OutputStream).TransmitFile(path, offset, count, fileShareMode, cancellationToken);
        }
    }
}