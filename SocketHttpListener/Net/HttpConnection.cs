using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    sealed class HttpConnection
    {
        const int BufferSize = 8192;
        IAcceptSocket sock;
        Stream stream;
        EndPointListener epl;
        MemoryStream ms;
        byte[] buffer;
        HttpListenerContext context;
        StringBuilder current_line;
        ListenerPrefix prefix;
        HttpRequestStream i_stream;
        Stream o_stream;
        bool chunked;
        int reuses;
        bool context_bound;
        bool secure;
        int s_timeout = 300000; // 90k ms for first request, 15k ms from then on
        IpEndPointInfo local_ep;
        HttpListener last_listener;
        int[] client_cert_errors;
        ICertificate cert;
        Stream ssl_stream;

        private readonly ILogger _logger;
        private readonly ICryptoProvider _cryptoProvider;
        private readonly IMemoryStreamFactory _memoryStreamFactory;
        private readonly ITextEncoding _textEncoding;
        private readonly IStreamFactory _streamFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentInfo _environment;

        private HttpConnection(ILogger logger, IAcceptSocket sock, EndPointListener epl, bool secure, ICertificate cert, ICryptoProvider cryptoProvider, IStreamFactory streamFactory, IMemoryStreamFactory memoryStreamFactory, ITextEncoding textEncoding, IFileSystem fileSystem, IEnvironmentInfo environment)
        {
            _logger = logger;
            this.sock = sock;
            this.epl = epl;
            this.secure = secure;
            this.cert = cert;
            _cryptoProvider = cryptoProvider;
            _memoryStreamFactory = memoryStreamFactory;
            _textEncoding = textEncoding;
            _fileSystem = fileSystem;
            _environment = environment;
            _streamFactory = streamFactory;
        }

        private async Task InitStream()
        {
            if (secure == false)
            {
                stream = _streamFactory.CreateNetworkStream(sock, false);
            }
            else
            {
                //ssl_stream = epl.Listener.CreateSslStream(new NetworkStream(sock, false), false, (t, c, ch, e) =>
                //{
                //    if (c == null)
                //        return true;
                //    var c2 = c as X509Certificate2;
                //    if (c2 == null)
                //        c2 = new X509Certificate2(c.GetRawCertData());
                //    client_cert = c2;
                //    client_cert_errors = new int[] { (int)e };
                //    return true;
                //});
                //stream = ssl_stream.AuthenticatedStream;

                ssl_stream = _streamFactory.CreateSslStream(_streamFactory.CreateNetworkStream(sock, false), false);
                await _streamFactory.AuthenticateSslStreamAsServer(ssl_stream, cert).ConfigureAwait(false);
                stream = ssl_stream;
            }
            Init();
        }

        public static async Task<HttpConnection> Create(ILogger logger, IAcceptSocket sock, EndPointListener epl, bool secure, ICertificate cert, ICryptoProvider cryptoProvider, IStreamFactory streamFactory, IMemoryStreamFactory memoryStreamFactory, ITextEncoding textEncoding, IFileSystem fileSystem, IEnvironmentInfo environment)
        {
            var connection = new HttpConnection(logger, sock, epl, secure, cert, cryptoProvider, streamFactory, memoryStreamFactory, textEncoding, fileSystem, environment);

            await connection.InitStream().ConfigureAwait(false);

            return connection;
        }

        public Stream Stream
        {
            get
            {
                return stream;
            }
        }

        internal int[] ClientCertificateErrors
        {
            get { return client_cert_errors; }
        }

        void Init()
        {
            if (ssl_stream != null)
            {
                //ssl_stream.AuthenticateAsServer(client_cert, true, (SslProtocols)ServicePointManager.SecurityProtocol, false);
                //_streamFactory.AuthenticateSslStreamAsServer(ssl_stream, cert);
            }

            context_bound = false;
            i_stream = null;
            o_stream = null;
            prefix = null;
            chunked = false;
            ms = _memoryStreamFactory.CreateNew();
            position = 0;
            input_state = InputState.RequestLine;
            line_state = LineState.None;
            context = new HttpListenerContext(this, _logger, _cryptoProvider, _memoryStreamFactory, _textEncoding, _fileSystem);
        }

        public bool IsClosed
        {
            get { return (sock == null); }
        }

        public int Reuses
        {
            get { return reuses; }
        }

        public IpEndPointInfo LocalEndPoint
        {
            get
            {
                if (local_ep != null)
                    return local_ep;

                local_ep = (IpEndPointInfo)sock.LocalEndPoint;
                return local_ep;
            }
        }

        public IpEndPointInfo RemoteEndPoint
        {
            get { return (IpEndPointInfo)sock.RemoteEndPoint; }
        }

        public bool IsSecure
        {
            get { return secure; }
        }

        public ListenerPrefix Prefix
        {
            get { return prefix; }
            set { prefix = value; }
        }

        public async Task BeginReadRequest()
        {
            if (buffer == null)
                buffer = new byte[BufferSize];

            try
            {
                //if (reuses == 1)
                //    s_timeout = 15000;
                var nRead = await stream.ReadAsync(buffer, 0, BufferSize).ConfigureAwait(false);

                OnReadInternal(nRead);
            }
            catch (Exception ex)
            {
                OnReadInternalException(ms, ex);
            }
        }

        public HttpRequestStream GetRequestStream(bool chunked, long contentlength)
        {
            if (i_stream == null)
            {
                byte[] buffer;
                _memoryStreamFactory.TryGetBuffer(ms, out buffer);

                int length = (int)ms.Length;
                ms = null;
                if (chunked)
                {
                    this.chunked = true;
                    //context.Response.SendChunked = true;
                    i_stream = new ChunkedInputStream(context, stream, buffer, position, length - position);
                }
                else
                {
                    i_stream = new HttpRequestStream(stream, buffer, position, length - position, contentlength);
                }
            }
            return i_stream;
        }

        public Stream GetResponseStream(bool isExpect100Continue = false)
        {
            // TODO: can we get this stream before reading the input?
            if (o_stream == null)
            {
                //context.Response.DetermineIfChunked();

                var supportsDirectSocketAccess = !context.Response.SendChunked && !isExpect100Continue && !secure;

                //o_stream = new ResponseStream(stream, context.Response, _memoryStreamFactory, _textEncoding, _fileSystem, sock, supportsDirectSocketAccess, _logger, _environment);

                o_stream = new HttpResponseStream(stream, context.Response, false, _memoryStreamFactory, sock, supportsDirectSocketAccess, _environment, _fileSystem, _logger);
            }
            return o_stream;
        }

        void OnReadInternal(int nread)
        {
            ms.Write(buffer, 0, nread);
            if (ms.Length > 32768)
            {
                SendError("Bad request", 400);
                Close(true);
                return;
            }

            if (nread == 0)
            {
                //if (ms.Length > 0)
                //	SendError (); // Why bother?
                CloseSocket();
                Unbind();
                return;
            }

            if (ProcessInput(ms))
            {
                if (!context.HaveError)
                    context.Request.FinishInitialization();

                if (context.HaveError)
                {
                    SendError();
                    Close(true);
                    return;
                }

                if (!epl.BindContext(context))
                {
                    SendError("Invalid host", 400);
                    Close(true);
                    return;
                }
                HttpListener listener = epl.Listener;
                if (last_listener != listener)
                {
                    RemoveConnection();
                    listener.AddConnection(this);
                    last_listener = listener;
                }

                context_bound = true;
                listener.RegisterContext(context);
                return;
            }

            BeginReadRequest();
        }

        private void OnReadInternalException(MemoryStream ms, Exception ex)
        {
            //_logger.ErrorException("Error in HttpConnection.OnReadInternal", ex);

            if (ms != null && ms.Length > 0)
                SendError();
            if (sock != null)
            {
                CloseSocket();
                Unbind();
            }
        }

        void RemoveConnection()
        {
            if (last_listener == null)
                epl.RemoveConnection(this);
            else
                last_listener.RemoveConnection(this);
        }

        enum InputState
        {
            RequestLine,
            Headers
        }

        enum LineState
        {
            None,
            CR,
            LF
        }

        InputState input_state = InputState.RequestLine;
        LineState line_state = LineState.None;
        int position;

        // true -> done processing
        // false -> need more input
        bool ProcessInput(MemoryStream ms)
        {
            byte[] buffer;
            _memoryStreamFactory.TryGetBuffer(ms, out buffer);

            int len = (int)ms.Length;
            int used = 0;
            string line;

            while (true)
            {
                if (context.HaveError)
                    return true;

                if (position >= len)
                    break;

                try
                {
                    line = ReadLine(buffer, position, len - position, ref used);
                    position += used;
                }
                catch
                {
                    context.ErrorMessage = "Bad request";
                    context.ErrorStatus = 400;
                    return true;
                }

                if (line == null)
                    break;

                if (line == "")
                {
                    if (input_state == InputState.RequestLine)
                        continue;
                    current_line = null;
                    ms = null;
                    return true;
                }

                if (input_state == InputState.RequestLine)
                {
                    context.Request.SetRequestLine(line);
                    input_state = InputState.Headers;
                }
                else
                {
                    try
                    {
                        context.Request.AddHeader(line);
                    }
                    catch (Exception e)
                    {
                        context.ErrorMessage = e.Message;
                        context.ErrorStatus = 400;
                        return true;
                    }
                }
            }

            if (used == len)
            {
                ms.SetLength(0);
                position = 0;
            }
            return false;
        }

        string ReadLine(byte[] buffer, int offset, int len, ref int used)
        {
            if (current_line == null)
                current_line = new StringBuilder(128);
            int last = offset + len;
            used = 0;

            for (int i = offset; i < last && line_state != LineState.LF; i++)
            {
                used++;
                byte b = buffer[i];
                if (b == 13)
                {
                    line_state = LineState.CR;
                }
                else if (b == 10)
                {
                    line_state = LineState.LF;
                }
                else
                {
                    current_line.Append((char)b);
                }
            }

            string result = null;
            if (line_state == LineState.LF)
            {
                line_state = LineState.None;
                result = current_line.ToString();
                current_line.Length = 0;
            }

            return result;
        }

        public void SendError(string msg, int status)
        {
            try
            {
                HttpListenerResponse response = context.Response;
                response.StatusCode = status;
                response.ContentType = "text/html";
                string description = HttpListenerResponse.GetStatusDescription(status);
                string str;
                if (msg != null)
                    str = String.Format("<h1>{0} ({1})</h1>", description, msg);
                else
                    str = String.Format("<h1>{0}</h1>", description);

                byte[] error = context.Response.ContentEncoding.GetBytes(str);
                response.ContentLength64 = error.Length;
                response.OutputStream.Write(error, 0, (int)error.Length);
                response.Close();
            }
            catch
            {
                // response was already closed
            }
        }

        public void SendError()
        {
            SendError(context.ErrorMessage, context.ErrorStatus);
        }

        void Unbind()
        {
            if (context_bound)
            {
                epl.UnbindContext(context);
                context_bound = false;
            }
        }

        public void Close()
        {
            Close(false);
        }

        private void CloseSocket()
        {
            if (sock == null)
                return;

            try
            {
                sock.Close();
            }
            catch
            {
            }
            finally
            {
                sock = null;
            }
            RemoveConnection();
        }

        internal void Close(bool force_close)
        {
            if (sock != null)
            {
                if (!context.Request.IsWebSocketRequest || force_close)
                {
                    Stream st = GetResponseStream();
                    if (st != null)
                    {
                        st.Dispose();
                    }

                    o_stream = null;
                }
            }

            if (sock != null)
            {
                force_close |= !context.Request.KeepAlive;
                if (!force_close)
                    force_close = (string.Equals(context.Response.Headers["connection"], "close", StringComparison.OrdinalIgnoreCase));
                /*
				if (!force_close) {
//					bool conn_close = (status_code == 400 || status_code == 408 || status_code == 411 ||
//							status_code == 413 || status_code == 414 || status_code == 500 ||
//							status_code == 503);
					force_close |= (context.Request.ProtocolVersion <= HttpVersion.Version10);
				}
				*/

                if (!force_close && context.Request.FlushInput())
                {
                    reuses++;
                    Unbind();
                    Init();
                    BeginReadRequest();
                    return;
                }

                IAcceptSocket s = sock;
                sock = null;
                try
                {
                    if (s != null)
                        s.Shutdown(true);
                }
                catch
                {
                }
                finally
                {
                    if (s != null)
                        s.Close();
                }
                Unbind();
                RemoveConnection();
                return;
            }
        }
    }
}