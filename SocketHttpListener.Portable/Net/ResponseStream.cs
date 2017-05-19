using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    // FIXME: Does this buffer the response until Close?
    // Update: we send a single packet for the first non-chunked Write
    // What happens when we set content-length to X and write X-1 bytes then close?
    // what if we don't set content-length at all?
    public class ResponseStream : Stream
    {
        HttpListenerResponse response;
        bool disposed;
        bool trailer_sent;
        Stream stream;
        private readonly IMemoryStreamFactory _memoryStreamFactory;
        private readonly ITextEncoding _textEncoding;
        private readonly IFileSystem _fileSystem;
        private readonly IAcceptSocket _socket;
        private readonly bool _supportsDirectSocketAccess;
        private readonly ILogger _logger;
        private readonly IEnvironmentInfo _environment;

        internal ResponseStream(Stream stream, HttpListenerResponse response, IMemoryStreamFactory memoryStreamFactory, ITextEncoding textEncoding, IFileSystem fileSystem, IAcceptSocket socket, bool supportsDirectSocketAccess, ILogger logger, IEnvironmentInfo environment)
        {
            this.response = response;
            _memoryStreamFactory = memoryStreamFactory;
            _textEncoding = textEncoding;
            _fileSystem = fileSystem;
            _socket = socket;
            _supportsDirectSocketAccess = supportsDirectSocketAccess;
            _logger = logger;
            _environment = environment;
            this.stream = stream;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposed == false)
            {
                disposed = true;
                using (var ms = GetHeaders(response, _memoryStreamFactory, false))
                {
                    if (stream.CanWrite)
                    {
                        try
                        {
                            bool chunked = response.SendChunked;

                            if (ms != null)
                            {
                                var start = ms.Position;
                                if (chunked && !trailer_sent)
                                {
                                    trailer_sent = true;
                                    var bytes = GetChunkSizeBytes(0, true);
                                    ms.Position = ms.Length;
                                    ms.Write(bytes, 0, bytes.Length);
                                    ms.Position = start;
                                }

                                ms.CopyTo(stream);
                            }
                            else if (chunked && !trailer_sent)
                            {
                                trailer_sent = true;

                                var bytes = GetChunkSizeBytes(0, true);
                                stream.Write(bytes, 0, bytes.Length);
                            }
                        }
                        catch (IOException ex)
                        {
                            // Ignore error due to connection reset by peer
                        }
                    }
                    response.Close();
                }
            }

            base.Dispose(disposing);
        }

        internal static MemoryStream GetHeaders(HttpListenerResponse response, IMemoryStreamFactory memoryStreamFactory, bool closing)
        {
            // SendHeaders works on shared headers
            lock (response.headers_lock)
            {
                if (response.HeadersSent)
                    return null;
                var ms = memoryStreamFactory.CreateNew();
                response.SendHeaders(closing, ms);
                return ms;
            }
        }

        public override void Flush()
        {
        }

        static byte[] crlf = new byte[] { 13, 10 };
        byte[] GetChunkSizeBytes(int size, bool final)
        {
            string str = String.Format("{0:x}\r\n{1}", size, final ? "\r\n" : "");
            return _textEncoding.GetASCIIEncoding().GetBytes(str);
        }

        internal void InternalWrite(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        const int MsCopyBufferSize = 81920;
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (count == 0)
            {
                return;
            }

            using (var ms = GetHeaders(response, _memoryStreamFactory, false))
            {
                bool chunked = response.SendChunked;
                if (ms != null)
                {
                    long start = ms.Position; // After the possible preamble for the encoding
                    ms.Position = ms.Length;
                    if (chunked)
                    {
                        var bytes = GetChunkSizeBytes(count, false);
                        ms.Write(bytes, 0, bytes.Length);
                    }

                    ms.Write(buffer, offset, count);

                    if (chunked)
                    {
                        ms.Write(crlf, 0, 2);
                    }

                    ms.Position = start;
                    ms.CopyTo(stream, MsCopyBufferSize);

                    return;
                }

                if (chunked)
                {
                    var bytes = GetChunkSizeBytes(count, false);
                    stream.Write(bytes, 0, bytes.Length);
                }

                stream.Write(buffer, offset, count);

                if (chunked)
                    stream.Write(crlf, 0, 2);
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (count == 0)
            {
                return;
            }

            using (var ms = GetHeaders(response, _memoryStreamFactory, false))
            {
                bool chunked = response.SendChunked;
                if (ms != null)
                {
                    long start = ms.Position; // After the possible preamble for the encoding
                    ms.Position = ms.Length;
                    if (chunked)
                    {
                        var bytes = GetChunkSizeBytes(count, false);
                        ms.Write(bytes, 0, bytes.Length);
                    }

                    ms.Write(buffer, offset, count);

                    if (chunked)
                    {
                        ms.Write(crlf, 0, 2);
                    }

                    ms.Position = start;
                    await ms.CopyToAsync(stream, MsCopyBufferSize, cancellationToken).ConfigureAwait(false);

                    return;
                }

                if (chunked)
                {
                    var bytes = GetChunkSizeBytes(count, false);
                    stream.Write(bytes, 0, bytes.Length);
                }

                await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);

                if (chunked)
                    stream.Write(crlf, 0, 2);
            }
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        private bool EnableSendFileWithSocket
        {
            get { return false; }
        }

        public Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken)
        {
            if (_supportsDirectSocketAccess && offset == 0 && count == 0 && !response.SendChunked && response.ContentLength64 > 8192)
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
            var ms = GetHeaders(response, _memoryStreamFactory, false);

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

            _logger.Info("Socket sending file {0} {1}", path, response.ContentLength64);
            return _socket.SendFile(path, preBuffer, _emptyBuffer, cancellationToken);
        }

        private async Task TransmitFileManaged(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken)
        {
            var allowAsync = _environment.OperatingSystem != OperatingSystem.Windows;

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
                        await fs.CopyToAsync(targetStream, 81920, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        fs.CopyTo(targetStream, 81920);
                    }
                }
            }
        }

        private static async Task CopyToInternalAsyncWithSyncRead(Stream source, Stream destination, long copyLength, CancellationToken cancellationToken)
        {
            var array = new byte[81920];
            int bytesRead;

            while ((bytesRead = source.Read(array, 0, array.Length)) != 0)
            {
                if (bytesRead == 0)
                {
                    break;
                }

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
            var array = new byte[81920];
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                if (bytesRead == 0)
                {
                    break;
                }

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
