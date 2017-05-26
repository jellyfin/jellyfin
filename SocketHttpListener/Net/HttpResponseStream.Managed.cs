using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.System;

namespace SocketHttpListener.Net
{
    // Licensed to the .NET Foundation under one or more agreements.
    // See the LICENSE file in the project root for more information.
    //
    // System.Net.ResponseStream
    //
    // Author:
    //	Gonzalo Paniagua Javier (gonzalo@novell.com)
    //
    // Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
    //
    // Permission is hereby granted, free of charge, to any person obtaining
    // a copy of this software and associated documentation files (the
    // "Software"), to deal in the Software without restriction, including
    // without limitation the rights to use, copy, modify, merge, publish,
    // distribute, sublicense, and/or sell copies of the Software, and to
    // permit persons to whom the Software is furnished to do so, subject to
    // the following conditions:
    // 
    // The above copyright notice and this permission notice shall be
    // included in all copies or substantial portions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    // EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    // MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    // NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    // LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    // OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    // WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    //

    internal partial class HttpResponseStream : Stream
    {
        private HttpListenerResponse _response;
        private bool _ignore_errors;
        private bool _trailer_sent;
        private Stream _stream;
        private readonly IMemoryStreamFactory _memoryStreamFactory;
        private readonly IAcceptSocket _socket;
        private readonly bool _supportsDirectSocketAccess;
        private readonly IEnvironmentInfo _environment;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        internal HttpResponseStream(Stream stream, HttpListenerResponse response, bool ignore_errors, IMemoryStreamFactory memoryStreamFactory, IAcceptSocket socket, bool supportsDirectSocketAccess, IEnvironmentInfo environment, IFileSystem fileSystem, ILogger logger)
        {
            _response = response;
            _ignore_errors = ignore_errors;
            _memoryStreamFactory = memoryStreamFactory;
            _socket = socket;
            _supportsDirectSocketAccess = supportsDirectSocketAccess;
            _environment = environment;
            _fileSystem = fileSystem;
            _logger = logger;
            _stream = stream;
        }

        private void DisposeCore()
        {
            byte[] bytes = null;
            MemoryStream ms = GetHeaders(true);
            bool chunked = _response.SendChunked;
            if (_stream.CanWrite)
            {
                try
                {
                    if (ms != null)
                    {
                        long start = ms.Position;
                        if (chunked && !_trailer_sent)
                        {
                            bytes = GetChunkSizeBytes(0, true);
                            ms.Position = ms.Length;
                            ms.Write(bytes, 0, bytes.Length);
                        }
                        InternalWrite(ms.GetBuffer(), (int)start, (int)(ms.Length - start));
                        _trailer_sent = true;
                    }
                    else if (chunked && !_trailer_sent)
                    {
                        bytes = GetChunkSizeBytes(0, true);
                        InternalWrite(bytes, 0, bytes.Length);
                        _trailer_sent = true;
                    }
                }
                catch (HttpListenerException)
                {
                    // Ignore error due to connection reset by peer
                }
            }
            _response.Close();
        }

        internal async Task WriteWebSocketHandshakeHeadersAsync()
        {
            if (_closed)
                throw new ObjectDisposedException(GetType().ToString());

            if (_stream.CanWrite)
            {
                MemoryStream ms = GetHeaders(closing: false, isWebSocketHandshake: true);
                bool chunked = _response.SendChunked;

                long start = ms.Position;
                if (chunked)
                {
                    byte[] bytes = GetChunkSizeBytes(0, true);
                    ms.Position = ms.Length;
                    ms.Write(bytes, 0, bytes.Length);
                }

                await InternalWriteAsync(ms.GetBuffer(), (int)start, (int)(ms.Length - start)).ConfigureAwait(false);
                await _stream.FlushAsync().ConfigureAwait(false);
            }
        }

        private MemoryStream GetHeaders(bool closing, bool isWebSocketHandshake = false)
        {
            // SendHeaders works on shared headers
            lock (_response.headers_lock)
            {
                if (_response.HeadersSent)
                    return null;
                var ms = _memoryStreamFactory.CreateNew();
                _response.SendHeaders(closing, ms);
                return ms;
            }

            //lock (_response._headersLock)
            //{
            //    if (_response.SentHeaders)
            //    {
            //        return null;
            //    }

            //    MemoryStream ms = new MemoryStream();
            //    _response.SendHeaders(closing, ms, isWebSocketHandshake);
            //    return ms;
            //}
        }

        private static byte[] s_crlf = new byte[] { 13, 10 };
        private static byte[] GetChunkSizeBytes(int size, bool final)
        {
            string str = String.Format("{0:x}\r\n{1}", size, final ? "\r\n" : "");
            return Encoding.ASCII.GetBytes(str);
        }

        internal void InternalWrite(byte[] buffer, int offset, int count)
        {
            if (_ignore_errors)
            {
                try
                {
                    _stream.Write(buffer, offset, count);
                }
                catch { }
            }
            else
            {
                _stream.Write(buffer, offset, count);
            }
        }

        internal Task InternalWriteAsync(byte[] buffer, int offset, int count) =>
            _ignore_errors ? InternalWriteIgnoreErrorsAsync(buffer, offset, count) : _stream.WriteAsync(buffer, offset, count);

        private async Task InternalWriteIgnoreErrorsAsync(byte[] buffer, int offset, int count)
        {
            try { await _stream.WriteAsync(buffer, offset, count).ConfigureAwait(false); }
            catch { }
        }

        private void WriteCore(byte[] buffer, int offset, int size)
        {
            if (size == 0)
                return;

            byte[] bytes = null;
            MemoryStream ms = GetHeaders(false);
            bool chunked = _response.SendChunked;
            if (ms != null)
            {
                long start = ms.Position; // After the possible preamble for the encoding
                ms.Position = ms.Length;
                if (chunked)
                {
                    bytes = GetChunkSizeBytes(size, false);
                    ms.Write(bytes, 0, bytes.Length);
                }

                int new_count = Math.Min(size, 16384 - (int)ms.Position + (int)start);
                ms.Write(buffer, offset, new_count);
                size -= new_count;
                offset += new_count;
                InternalWrite(ms.GetBuffer(), (int)start, (int)(ms.Length - start));
                ms.SetLength(0);
                ms.Capacity = 0; // 'dispose' the buffer in ms.
            }
            else if (chunked)
            {
                bytes = GetChunkSizeBytes(size, false);
                InternalWrite(bytes, 0, bytes.Length);
            }

            if (size > 0)
                InternalWrite(buffer, offset, size);
            if (chunked)
                InternalWrite(s_crlf, 0, 2);
        }

        private IAsyncResult BeginWriteCore(byte[] buffer, int offset, int size, AsyncCallback cback, object state)
        {
            if (_closed)
            {
                HttpStreamAsyncResult ares = new HttpStreamAsyncResult(this);
                ares._callback = cback;
                ares._state = state;
                ares.Complete();
                return ares;
            }

            byte[] bytes = null;
            MemoryStream ms = GetHeaders(false);
            bool chunked = _response.SendChunked;
            if (ms != null)
            {
                long start = ms.Position;
                ms.Position = ms.Length;
                if (chunked)
                {
                    bytes = GetChunkSizeBytes(size, false);
                    ms.Write(bytes, 0, bytes.Length);
                }
                ms.Write(buffer, offset, size);
                buffer = ms.GetBuffer();
                offset = (int)start;
                size = (int)(ms.Position - start);
            }
            else if (chunked)
            {
                bytes = GetChunkSizeBytes(size, false);
                InternalWrite(bytes, 0, bytes.Length);
            }

            return _stream.BeginWrite(buffer, offset, size, cback, state);
        }

        private void EndWriteCore(IAsyncResult asyncResult)
        {
            if (_closed)
                return;

            if (_ignore_errors)
            {
                try
                {
                    _stream.EndWrite(asyncResult);
                    if (_response.SendChunked)
                        _stream.Write(s_crlf, 0, 2);
                }
                catch { }
            }
            else
            {
                _stream.EndWrite(asyncResult);
                if (_response.SendChunked)
                    _stream.Write(s_crlf, 0, 2);
            }
        }

        private bool EnableSendFileWithSocket = false;

        public Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken)
        {
            if (_supportsDirectSocketAccess && offset == 0 && count == 0 && !_response.SendChunked && _response.ContentLength64 > 8192)
            {
                if (EnableSendFileWithSocket)
                {
                    return TransmitFileOverSocket(path, offset, count, fileShareMode, cancellationToken);
                }
            }
            return TransmitFileManaged(path, offset, count, fileShareMode, cancellationToken);
        }

        private readonly byte[] _emptyBuffer = new byte[] { };
        private Task TransmitFileOverSocket(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken)
        {
            var ms = GetHeaders(false);

            byte[] preBuffer;
            if (ms != null)
            {
                using (var msCopy = new MemoryStream())
                {
                    ms.CopyTo(msCopy);
                    preBuffer = msCopy.ToArray();
                }
            }
            else
            {
                return TransmitFileManaged(path, offset, count, fileShareMode, cancellationToken);
            }

            //_logger.Info("Socket sending file {0} {1}", path, response.ContentLength64);

            var taskCompletion = new TaskCompletionSource<bool>();

            Action<IAsyncResult> callback = callbackResult =>
            {
                try
                {
                    _socket.EndSendFile(callbackResult);
                    taskCompletion.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    taskCompletion.TrySetException(ex);
                }
            };

            var result = _socket.BeginSendFile(path, preBuffer, _emptyBuffer, new AsyncCallback(callback), null);

            if (result.CompletedSynchronously)
            {
                callback(result);
            }

            cancellationToken.Register(() => taskCompletion.TrySetCanceled());

            return taskCompletion.Task;
        }

        const int StreamCopyToBufferSize = 81920;
        private async Task TransmitFileManaged(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken)
        {
            var allowAsync = _environment.OperatingSystem != MediaBrowser.Model.System.OperatingSystem.Windows;

            //if (count <= 0)
            //{
            //    allowAsync = true;
            //}

            var fileOpenOptions = offset > 0
                ? FileOpenOptions.RandomAccess
                : FileOpenOptions.SequentialScan;

            if (allowAsync)
            {
                fileOpenOptions |= FileOpenOptions.Asynchronous;
            }

            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039

            using (var fs = _fileSystem.GetFileStream(path, FileOpenMode.Open, FileAccessMode.Read, fileShareMode, fileOpenOptions))
            {
                if (offset > 0)
                {
                    fs.Position = offset;
                }

                var targetStream = this;

                if (count > 0)
                {
                    if (allowAsync)
                    {
                        await CopyToInternalAsync(fs, targetStream, count, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await CopyToInternalAsyncWithSyncRead(fs, targetStream, count, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (allowAsync)
                    {
                        await fs.CopyToAsync(targetStream, StreamCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        fs.CopyTo(targetStream, StreamCopyToBufferSize);
                    }
                }
            }
        }

        private static async Task CopyToInternalAsyncWithSyncRead(Stream source, Stream destination, long copyLength, CancellationToken cancellationToken)
        {
            var array = new byte[StreamCopyToBufferSize];
            int bytesRead;

            while ((bytesRead = source.Read(array, 0, array.Length)) != 0)
            {
                var bytesToWrite = Math.Min(bytesRead, copyLength);

                if (bytesToWrite > 0)
                {
                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToWrite), cancellationToken).ConfigureAwait(false);
                }

                copyLength -= bytesToWrite;

                if (copyLength <= 0)
                {
                    break;
                }
            }
        }

        private static async Task CopyToInternalAsync(Stream source, Stream destination, long copyLength, CancellationToken cancellationToken)
        {
            var array = new byte[StreamCopyToBufferSize];
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                var bytesToWrite = Math.Min(bytesRead, copyLength);

                if (bytesToWrite > 0)
                {
                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToWrite), cancellationToken).ConfigureAwait(false);
                }

                copyLength -= bytesToWrite;

                if (copyLength <= 0)
                {
                    break;
                }
            }
        }
    }
}
