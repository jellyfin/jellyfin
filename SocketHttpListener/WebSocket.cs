using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketHttpListener.Net.WebSockets;
using HttpStatusCode = SocketHttpListener.Net.HttpStatusCode;
using WebSocketState = System.Net.WebSockets.WebSocketState;

namespace SocketHttpListener
{
    /// <summary>
    /// Implements the WebSocket interface.
    /// </summary>
    /// <remarks>
    /// The WebSocket class provides a set of methods and properties for two-way communication using
    /// the WebSocket protocol (<see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>).
    /// </remarks>
    public class WebSocket : IDisposable
    {
        #region Private Fields

        private Action _closeContext;
        private CompressionMethod _compression;
        private WebSocketContext _context;
        private CookieCollection _cookies;
        private AutoResetEvent _exitReceiving;
        private object _forConn;
        private readonly SemaphoreSlim _forEvent = new SemaphoreSlim(1, 1);
        private object _forMessageEventQueue;
        private readonly SemaphoreSlim _forSend = new SemaphoreSlim(1, 1);
        private const string _guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private Queue<MessageEventArgs> _messageEventQueue;
        private string _protocol;
        private volatile WebSocketState _readyState;
        private AutoResetEvent _receivePong;
        private bool _secure;
        private Stream _stream;
        private const string _version = "13";

        #endregion

        #region Internal Fields

        internal const int FragmentLength = 1016; // Max value is int.MaxValue - 14.

        #endregion

        #region Internal Constructors

        // As server
        internal WebSocket(string protocol)
        {
            _protocol = protocol;
        }

        public void SetContext(HttpListenerWebSocketContext context, Action closeContextFn, Stream stream)
        {
            _context = context;

            _closeContext = closeContextFn;
            _secure = context.IsSecureConnection;
            _stream = stream;

            init();
        }

        // In the .NET Framework, this pulls the value from a P/Invoke.  Here we just hardcode it to a reasonable default.
        public static TimeSpan DefaultKeepAliveInterval => TimeSpan.FromSeconds(30);

        #endregion

        /// <summary>
        /// Gets the state of the WebSocket connection.
        /// </summary>
        /// <value>
        /// One of the <see cref="WebSocketState"/> enum values, indicates the state of the WebSocket
        /// connection. The default value is <see cref="WebSocketState.Connecting"/>.
        /// </value>
        public WebSocketState ReadyState => _readyState;

        #region Public Events

        /// <summary>
        /// Occurs when the WebSocket connection has been closed.
        /// </summary>
        public event EventHandler<CloseEventArgs> OnClose;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> gets an error.
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> receives a message.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when the WebSocket connection has been established.
        /// </summary>
        public event EventHandler OnOpen;

        #endregion

        #region Private Methods

        private async Task CloseAsync(CloseStatusCode code, string reason, bool wait)
        {
            await CloseAsync(new PayloadData(
                await ((ushort)code).AppendAsync(reason).ConfigureAwait(false)),
                !code.IsReserved(),
                wait).ConfigureAwait(false);
        }

        private async Task CloseAsync(PayloadData payload, bool send, bool wait)
        {
            lock (_forConn)
            {
                if (_readyState == WebSocketState.CloseSent || _readyState == WebSocketState.Closed)
                {
                    return;
                }

                _readyState = WebSocketState.CloseSent;
            }

            var e = new CloseEventArgs(payload)
            {
                WasClean = await CloseHandshakeAsync(
                  send ? WebSocketFrame.CreateCloseFrame(Mask.Unmask, payload).ToByteArray() : null,
                  wait ? 1000 : 0).ConfigureAwait(false)
            };

            _readyState = WebSocketState.Closed;
            try
            {
                OnClose.Emit(this, e);
            }
            catch (Exception ex)
            {
                error("An exception has occurred while OnClose.", ex);
            }
        }

        private async Task<bool> CloseHandshakeAsync(byte[] frameAsBytes, int millisecondsTimeout)
        {
            var sent = frameAsBytes != null && await WriteBytesAsync(frameAsBytes).ConfigureAwait(false);
            var received =
              millisecondsTimeout == 0 ||
              (sent && _exitReceiving != null && _exitReceiving.WaitOne(millisecondsTimeout));

            closeServerResources();

            if (_receivePong != null)
            {
                _receivePong.Dispose();
                _receivePong = null;
            }

            if (_exitReceiving != null)
            {
                _exitReceiving.Dispose();
                _exitReceiving = null;
            }

            var result = sent && received;

            return result;
        }

        // As server
        private void closeServerResources()
        {
            if (_closeContext == null)
                return;

            try
            {
                _closeContext();
            }
            catch (SocketException)
            {
                // it could be unable to send the handshake response
            }

            _closeContext = null;
            _stream = null;
            _context = null;
        }

        private async Task<bool> ConcatenateFragmentsIntoAsync(Stream dest)
        {
            while (true)
            {
                var frame = await WebSocketFrame.ReadAsync(_stream, true).ConfigureAwait(false);
                if (frame.IsFinal)
                {
                    /* FINAL */

                    // CONT
                    if (frame.IsContinuation)
                    {
                        dest.WriteBytes(frame.PayloadData.ApplicationData);
                        break;
                    }

                    // PING
                    if (frame.IsPing)
                    {
                        processPingFrame(frame);
                        continue;
                    }

                    // PONG
                    if (frame.IsPong)
                    {
                        processPongFrame(frame);
                        continue;
                    }

                    // CLOSE
                    if (frame.IsClose)
                        return await ProcessCloseFrameAsync(frame).ConfigureAwait(false);
                }
                else
                {
                    /* MORE */

                    // CONT
                    if (frame.IsContinuation)
                    {
                        dest.WriteBytes(frame.PayloadData.ApplicationData);
                        continue;
                    }
                }

                // ?
                return await ProcessUnsupportedFrameAsync(
                  frame,
                  CloseStatusCode.IncorrectData,
                  "An incorrect data has been received while receiving fragmented data.").ConfigureAwait(false);
            }

            return true;
        }

        // As server
        private HttpResponse createHandshakeCloseResponse(HttpStatusCode code)
        {
            var res = HttpResponse.CreateCloseResponse(code);
            res.Headers["Sec-WebSocket-Version"] = _version;

            return res;
        }

        private MessageEventArgs dequeueFromMessageEventQueue()
        {
            lock (_forMessageEventQueue)
                return _messageEventQueue.Count > 0
                       ? _messageEventQueue.Dequeue()
                       : null;
        }

        private void enqueueToMessageEventQueue(MessageEventArgs e)
        {
            lock (_forMessageEventQueue)
                _messageEventQueue.Enqueue(e);
        }

        private void error(string message, Exception exception)
        {
            try
            {
                if (exception != null)
                {
                    message += ". Exception.Message: " + exception.Message;
                }
                OnError.Emit(this, new ErrorEventArgs(message));
            }
            catch (Exception)
            {
            }
        }

        private void error(string message)
        {
            try
            {
                OnError.Emit(this, new ErrorEventArgs(message));
            }
            catch (Exception)
            {
            }
        }

        private void init()
        {
            _compression = CompressionMethod.None;
            _cookies = new CookieCollection();
            _forConn = new object();
            _messageEventQueue = new Queue<MessageEventArgs>();
            _forMessageEventQueue = ((ICollection)_messageEventQueue).SyncRoot;
            _readyState = WebSocketState.Connecting;
        }

        private async Task OpenAsync()
        {
            try
            {
                startReceiving();

            }
            catch (Exception ex)
            {
                await ProcessExceptionAsync(ex, "An exception has occurred while opening.").ConfigureAwait(false);
            }

            await _forEvent.WaitAsync().ConfigureAwait(false);
            try
            {
                OnOpen?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                await ProcessExceptionAsync(ex, "An exception has occurred while OnOpen.").ConfigureAwait(false);
            }
            finally
            {
                _forEvent.Release();
            }
        }

        private async Task<bool> ProcessCloseFrameAsync(WebSocketFrame frame)
        {
            var payload = frame.PayloadData;
            await CloseAsync(payload, !payload.ContainsReservedCloseStatusCode, false).ConfigureAwait(false);

            return false;
        }

        private bool processDataFrame(WebSocketFrame frame)
        {
            var e = frame.IsCompressed
                    ? new MessageEventArgs(
                        frame.Opcode, frame.PayloadData.ApplicationData.Decompress(_compression))
                    : new MessageEventArgs(frame.Opcode, frame.PayloadData);

            enqueueToMessageEventQueue(e);
            return true;
        }

        private async Task ProcessExceptionAsync(Exception exception, string message)
        {
            var code = CloseStatusCode.Abnormal;
            var reason = message;
            if (exception is WebSocketException)
            {
                var wsex = (WebSocketException)exception;
                code = wsex.Code;
                reason = wsex.Message;
            }

            error(message ?? code.GetMessage(), exception);
            if (_readyState == WebSocketState.Connecting)
            {
                await CloseAsync(HttpStatusCode.BadRequest).ConfigureAwait(false);
            }
            else
            {
                await CloseAsync(code, reason ?? code.GetMessage(), false).ConfigureAwait(false);
            }
        }

        private Task<bool> ProcessFragmentedFrameAsync(WebSocketFrame frame)
        {
            return frame.IsContinuation // Not first fragment
                   ? Task.FromResult(true)
                   : ProcessFragmentsAsync(frame);
        }

        private async Task<bool> ProcessFragmentsAsync(WebSocketFrame first)
        {
            using (var buff = new MemoryStream())
            {
                buff.WriteBytes(first.PayloadData.ApplicationData);
                if (!await ConcatenateFragmentsIntoAsync(buff).ConfigureAwait(false))
                {
                    return false;
                }

                byte[] data;
                if (_compression != CompressionMethod.None)
                {
                    data = buff.DecompressToArray(_compression);
                }
                else
                {
                    data = buff.ToArray();
                }

                enqueueToMessageEventQueue(new MessageEventArgs(first.Opcode, data));
                return true;
            }
        }

        private bool processPingFrame(WebSocketFrame frame)
        {
            return true;
        }

        private bool processPongFrame(WebSocketFrame frame)
        {
            _receivePong.Set();

            return true;
        }

        private async Task<bool> ProcessUnsupportedFrameAsync(WebSocketFrame frame, CloseStatusCode code, string reason)
        {
            await ProcessExceptionAsync(new WebSocketException(code, reason), null).ConfigureAwait(false);

            return false;
        }

        private Task<bool> ProcessWebSocketFrameAsync(WebSocketFrame frame)
        {
            return frame.IsCompressed && _compression == CompressionMethod.None
                   ? ProcessUnsupportedFrameAsync(
                       frame,
                       CloseStatusCode.IncorrectData,
                       "A compressed data has been received without available decompression method.")
                   : frame.IsFragmented
                     ? ProcessFragmentedFrameAsync(frame)
                     : frame.IsData
                       ? Task.FromResult(processDataFrame(frame))
                       : frame.IsPing
                         ? Task.FromResult(processPingFrame(frame))
                         : frame.IsPong
                           ? Task.FromResult(processPongFrame(frame))
                           : frame.IsClose
                             ? ProcessCloseFrameAsync(frame)
                             : ProcessUnsupportedFrameAsync(frame, CloseStatusCode.PolicyViolation, null);
        }

        private async Task<bool> SendAsync(Opcode opcode, Stream stream)
        {
            await _forSend.WaitAsync().ConfigureAwait(false);
            try
            {
                var src = stream;
                var compressed = false;
                var sent = false;
                try
                {
                    if (_compression != CompressionMethod.None)
                    {
                        stream = stream.Compress(_compression);
                        compressed = true;
                    }

                    sent = await SendAsync(opcode, Mask.Unmask, stream, compressed).ConfigureAwait(false);
                    if (!sent)
                        error("Sending a data has been interrupted.");
                }
                catch (Exception ex)
                {
                    error("An exception has occurred while sending a data.", ex);
                }
                finally
                {
                    if (compressed)
                        stream.Dispose();

                    src.Dispose();
                }

                return sent;
            }
            finally
            {
                _forSend.Release();
            }
        }

        private async Task<bool> SendAsync(Opcode opcode, Mask mask, Stream stream, bool compressed)
        {
            var len = stream.Length;

            /* Not fragmented */

            if (len == 0)
                return await SendAsync(Fin.Final, opcode, mask, new byte[0], compressed).ConfigureAwait(false);

            var quo = len / FragmentLength;
            var rem = (int)(len % FragmentLength);

            byte[] buff = null;
            if (quo == 0)
            {
                buff = new byte[rem];
                return await stream.ReadAsync(buff, 0, rem).ConfigureAwait(false) == rem &&
                       await SendAsync(Fin.Final, opcode, mask, buff, compressed).ConfigureAwait(false);
            }

            buff = new byte[FragmentLength];
            if (quo == 1 && rem == 0)
                return await stream.ReadAsync(buff, 0, FragmentLength).ConfigureAwait(false) == FragmentLength &&
                       await SendAsync(Fin.Final, opcode, mask, buff, compressed).ConfigureAwait(false);

            /* Send fragmented */

            // Begin
            if (await stream.ReadAsync(buff, 0, FragmentLength).ConfigureAwait(false) != FragmentLength ||
                !await SendAsync(Fin.More, opcode, mask, buff, compressed).ConfigureAwait(false))
                return false;

            var n = rem == 0 ? quo - 2 : quo - 1;
            for (long i = 0; i < n; i++)
                if (await stream.ReadAsync(buff, 0, FragmentLength).ConfigureAwait(false) != FragmentLength ||
                    !await SendAsync(Fin.More, Opcode.Cont, mask, buff, compressed).ConfigureAwait(false))
                    return false;

            // End
            if (rem == 0)
                rem = FragmentLength;
            else
                buff = new byte[rem];

            return await stream.ReadAsync(buff, 0, rem).ConfigureAwait(false) == rem &&
                   await SendAsync(Fin.Final, Opcode.Cont, mask, buff, compressed).ConfigureAwait(false);
        }

        private Task<bool> SendAsync(Fin fin, Opcode opcode, Mask mask, byte[] data, bool compressed)
        {
            lock (_forConn)
            {
                if (_readyState != WebSocketState.Open)
                {
                    return Task.FromResult(false);
                }

                return WriteBytesAsync(
                  WebSocketFrame.CreateWebSocketFrame(fin, opcode, mask, data, compressed).ToByteArray());
            }
        }

        // As server
        private Task<bool> SendHttpResponseAsync(HttpResponse response)
            => WriteBytesAsync(response.ToByteArray());

        private void startReceiving()
        {
            if (_messageEventQueue.Count > 0)
            {
                _messageEventQueue.Clear();
            }

            _exitReceiving = new AutoResetEvent(false);
            _receivePong = new AutoResetEvent(false);

            Action receive = null;
            receive = async () => await WebSocketFrame.ReadAsync(
                _stream,
                true,
                async frame =>
                {
                    if (await ProcessWebSocketFrameAsync(frame).ConfigureAwait(false) && _readyState != WebSocketState.Closed)
                    {
                        receive();

                        if (!frame.IsData)
                        {
                            return;
                        }

                        await _forEvent.WaitAsync().ConfigureAwait(false);

                        try
                        {
                            var e = dequeueFromMessageEventQueue();
                            if (e != null && _readyState == WebSocketState.Open)
                            {
                                OnMessage.Emit(this, e);
                            }
                        }
                        catch (Exception ex)
                        {
                            await ProcessExceptionAsync(ex, "An exception has occurred while OnMessage.").ConfigureAwait(false);
                        }
                        finally
                        {
                            _forEvent.Release();
                        }

                    }
                    else if (_exitReceiving != null)
                    {
                        _exitReceiving.Set();
                    }
                },
                async ex => await ProcessExceptionAsync(ex, "An exception has occurred while receiving a message.")).ConfigureAwait(false);

            receive();
        }

        private async Task<bool> WriteBytesAsync(byte[] data)
        {
            try
            {
                await _stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Internal Methods

        // As server
        internal async Task CloseAsync(HttpResponse response)
        {
            _readyState = WebSocketState.CloseSent;
            await SendHttpResponseAsync(response).ConfigureAwait(false);

            closeServerResources();

            _readyState = WebSocketState.Closed;
        }

        // As server
        internal Task CloseAsync(HttpStatusCode code)
            => CloseAsync(createHandshakeCloseResponse(code));

        // As server
        public async Task ConnectAsServer()
        {
            try
            {
                _readyState = WebSocketState.Open;
                await OpenAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ProcessExceptionAsync(ex, "An exception has occurred while connecting.").ConfigureAwait(false);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Closes the WebSocket connection, and releases all associated resources.
        /// </summary>
        public Task CloseAsync()
        {
            var msg = _readyState.CheckIfClosable();
            if (msg != null)
            {
                error(msg);

                return Task.CompletedTask;
            }

            var send = _readyState == WebSocketState.Open;
            return CloseAsync(new PayloadData(), send, send);
        }

        /// <summary>
        /// Closes the WebSocket connection with the specified <see cref="CloseStatusCode"/>
        /// and <see cref="string"/>, and releases all associated resources.
        /// </summary>
        /// <remarks>
        /// This method emits a <see cref="OnError"/> event if the size
        /// of <paramref name="reason"/> is greater than 123 bytes.
        /// </remarks>
        /// <param name="code">
        /// One of the <see cref="CloseStatusCode"/> enum values, represents the status code
        /// indicating the reason for the close.
        /// </param>
        /// <param name="reason">
        /// A <see cref="string"/> that represents the reason for the close.
        /// </param>
        public async Task CloseAsync(CloseStatusCode code, string reason)
        {
            byte[] data = null;
            var msg = _readyState.CheckIfClosable() ??
                      (data = await ((ushort)code).AppendAsync(reason).ConfigureAwait(false)).CheckIfValidControlData("reason");

            if (msg != null)
            {
                error(msg);

                return;
            }

            var send = _readyState == WebSocketState.Open && !code.IsReserved();
            await CloseAsync(new PayloadData(data), send, send).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a binary <paramref name="data"/> asynchronously using the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method doesn't wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        public Task SendAsync(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var msg = _readyState.CheckIfOpen();
            if (msg != null)
            {
                throw new Exception(msg);
            }

            return SendAsync(Opcode.Binary, new MemoryStream(data));
        }

        /// <summary>
        /// Sends a text <paramref name="data"/> asynchronously using the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method doesn't wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        public Task SendAsync(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var msg = _readyState.CheckIfOpen();
            if (msg != null)
            {
                throw new Exception(msg);
            }

            return SendAsync(Opcode.Text, new MemoryStream(Encoding.UTF8.GetBytes(data)));
        }

        #endregion

        #region Explicit Interface Implementation

        /// <summary>
        /// Closes the WebSocket connection, and releases all associated resources.
        /// </summary>
        /// <remarks>
        /// This method closes the WebSocket connection with <see cref="CloseStatusCode.Away"/>.
        /// </remarks>
        void IDisposable.Dispose()
        {
            CloseAsync(CloseStatusCode.Away, null).GetAwaiter().GetResult();
        }

        #endregion
    }
}
