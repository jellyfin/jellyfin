using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;
using System.Threading;
using MediaBrowser.Model.IO;

namespace SocketHttpListener.Net
{
    public sealed partial class HttpListenerResponse : IDisposable
    {
        private long _contentLength;
        private Version _version = HttpVersion.Version11;
        private int _statusCode = 200;
        internal object _headersLock = new object();
        private bool _forceCloseChunked;
        private ITextEncoding _textEncoding;

        internal HttpListenerResponse(HttpListenerContext context, ITextEncoding textEncoding)
        {
            _httpContext = context;
            _textEncoding = textEncoding;
        }

        internal bool ForceCloseChunked => _forceCloseChunked;

        private void EnsureResponseStream()
        {
            if (_responseStream == null)
            {
                _responseStream = _httpContext.Connection.GetResponseStream();
            }
        }

        public Version ProtocolVersion
        {
            get { return _version; }
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                {
                    throw new ArgumentException("Wrong version");
                }

                _version = new Version(value.Major, value.Minor); // match Windows behavior, trimming to just Major.Minor
            }
        }

        public int StatusCode
        {
            get { return _statusCode; }
            set
            {
                CheckDisposed();

                if (value < 100 || value > 999)
                    throw new ProtocolViolationException("Invalid status");

                _statusCode = value;
            }
        }

        private void Dispose()
        {
            Close(true);
        }

        public void Close()
        {
            if (Disposed)
                return;

            Close(false);
        }

        public void Abort()
        {
            if (Disposed)
                return;

            Close(true);
        }

        private void Close(bool force)
        {
            Disposed = true;
            _httpContext.Connection.Close(force);
        }

        public void Close(byte[] responseEntity, bool willBlock)
        {
            CheckDisposed();

            if (responseEntity == null)
            {
                throw new ArgumentNullException(nameof(responseEntity));
            }

            if (!SentHeaders && _boundaryType != BoundaryType.Chunked)
            {
                ContentLength64 = responseEntity.Length;
            }

            if (willBlock)
            {
                try
                {
                    OutputStream.Write(responseEntity, 0, responseEntity.Length);
                }
                finally
                {
                    Close(false);
                }
            }
            else
            {
                OutputStream.BeginWrite(responseEntity, 0, responseEntity.Length, iar =>
                {
                    var thisRef = (HttpListenerResponse)iar.AsyncState;
                    try
                    {
                        thisRef.OutputStream.EndWrite(iar);
                    }
                    finally
                    {
                        thisRef.Close(false);
                    }
                }, this);
            }
        }

        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            _webHeaders.Clear();
            //_webHeaders.Add(templateResponse._webHeaders);
            _contentLength = templateResponse._contentLength;
            _statusCode = templateResponse._statusCode;
            _statusDescription = templateResponse._statusDescription;
            _keepAlive = templateResponse._keepAlive;
            _version = templateResponse._version;
        }

        internal void SendHeaders(bool closing, MemoryStream ms, bool isWebSocketHandshake = false)
        {
            if (!isWebSocketHandshake)
            {
                if (_webHeaders["Server"] == null)
                {
                    _webHeaders.Set("Server", "Microsoft-NetCore/2.0");
                }

                if (_webHeaders["Date"] == null)
                {
                    _webHeaders.Set("Date", DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));
                }

                if (_boundaryType == BoundaryType.None)
                {
                    if (HttpListenerRequest.ProtocolVersion <= HttpVersion.Version10)
                    {
                        _keepAlive = false;
                    }
                    else
                    {
                        _boundaryType = BoundaryType.Chunked;
                    }

                    if (CanSendResponseBody(_httpContext.Response.StatusCode))
                    {
                        _contentLength = -1;
                    }
                    else
                    {
                        _boundaryType = BoundaryType.ContentLength;
                        _contentLength = 0;
                    }
                }

                if (_boundaryType != BoundaryType.Chunked)
                {
                    if (_boundaryType != BoundaryType.ContentLength && closing)
                    {
                        _contentLength = CanSendResponseBody(_httpContext.Response.StatusCode) ? -1 : 0;
                    }

                    if (_boundaryType == BoundaryType.ContentLength)
                    {
                        _webHeaders.Set("Content-Length", _contentLength.ToString("D", CultureInfo.InvariantCulture));
                    }
                }

                /* Apache forces closing the connection for these status codes:
                 *	HttpStatusCode.BadRequest 		        400
                 *	HttpStatusCode.RequestTimeout 		    408
                 *	HttpStatusCode.LengthRequired 		    411
                 *	HttpStatusCode.RequestEntityTooLarge 	413
                 *	HttpStatusCode.RequestUriTooLong 	    414
                 *	HttpStatusCode.InternalServerError      500
                 *	HttpStatusCode.ServiceUnavailable 	    503
                 */
                bool conn_close = (_statusCode == (int)HttpStatusCode.BadRequest || _statusCode == (int)HttpStatusCode.RequestTimeout
                        || _statusCode == (int)HttpStatusCode.LengthRequired || _statusCode == (int)HttpStatusCode.RequestEntityTooLarge
                        || _statusCode == (int)HttpStatusCode.RequestUriTooLong || _statusCode == (int)HttpStatusCode.InternalServerError
                        || _statusCode == (int)HttpStatusCode.ServiceUnavailable);

                if (!conn_close)
                {
                    conn_close = !_httpContext.Request.KeepAlive;
                }

                // They sent both KeepAlive: true and Connection: close
                if (!_keepAlive || conn_close)
                {
                    _webHeaders.Set("Connection", "Close");
                    conn_close = true;
                }

                if (SendChunked)
                {
                    _webHeaders.Set("Transfer-Encoding", "Chunked");
                }

                int reuses = _httpContext.Connection.Reuses;
                if (reuses >= 100)
                {
                    _forceCloseChunked = true;
                    if (!conn_close)
                    {
                        _webHeaders.Set("Connection", "Close");
                        conn_close = true;
                    }
                }

                if (HttpListenerRequest.ProtocolVersion <= HttpVersion.Version10)
                {
                    if (_keepAlive)
                    {
                        Headers["Keep-Alive"] = "true";
                    }

                    if (!conn_close)
                    {
                        _webHeaders.Set("Connection", "Keep-Alive");
                    }
                }

                ComputeCookies();
            }

            Encoding encoding = _textEncoding.GetDefaultEncoding();
            StreamWriter writer = new StreamWriter(ms, encoding, 256);
            writer.Write("HTTP/1.1 {0} ", _statusCode); // "1.1" matches Windows implementation, which ignores the response version
            writer.Flush();
            byte[] statusDescriptionBytes = WebHeaderEncoding.GetBytes(StatusDescription);
            ms.Write(statusDescriptionBytes, 0, statusDescriptionBytes.Length);
            writer.Write("\r\n");

            writer.Write(FormatHeaders(_webHeaders));
            writer.Flush();
            int preamble = encoding.GetPreamble().Length;
            EnsureResponseStream();

            /* Assumes that the ms was at position 0 */
            ms.Position = preamble;
            SentHeaders = !isWebSocketHandshake;
        }

        private static bool HeaderCanHaveEmptyValue(string name) =>
            !string.Equals(name, "Location", StringComparison.OrdinalIgnoreCase);

        private static string FormatHeaders(WebHeaderCollection headers)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < headers.Count; i++)
            {
                string key = headers.GetKey(i);
                string[] values = headers.GetValues(i);

                int startingLength = sb.Length;

                sb.Append(key).Append(": ");
                bool anyValues = false;
                for (int j = 0; j < values.Length; j++)
                {
                    string value = values[j];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (anyValues)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(value);
                        anyValues = true;
                    }
                }

                if (anyValues || HeaderCanHaveEmptyValue(key))
                {
                    // Complete the header
                    sb.Append("\r\n");
                }
                else
                {
                    // Empty header; remove it.
                    sb.Length = startingLength;
                }
            }

            return sb.Append("\r\n").ToString();
        }

        private bool Disposed { get; set; }
        internal bool SentHeaders { get; set; }

        public Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken)
        {
            return ((HttpResponseStream)OutputStream).TransmitFile(path, offset, count, fileShareMode, cancellationToken);
        }
    }
}
