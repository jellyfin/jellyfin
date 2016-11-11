using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SocketHttpListener.Net
{
    class RequestStream : Stream
    {
        byte[] buffer;
        int offset;
        int length;
        long remaining_body;
        bool disposed;
        Stream stream;

        internal RequestStream(Stream stream, byte[] buffer, int offset, int length)
            : this(stream, buffer, offset, length, -1)
        {
        }

        internal RequestStream(Stream stream, byte[] buffer, int offset, int length, long contentlength)
        {
            this.stream = stream;
            this.buffer = buffer;
            this.offset = offset;
            this.length = length;
            this.remaining_body = contentlength;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
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
            disposed = true;
        }

        public override void Flush()
        {
        }


        // Returns 0 if we can keep reading from the base stream,
        // > 0 if we read something from the buffer.
        // -1 if we had a content length set and we finished reading that many bytes.
        int FillFromBuffer(byte[] buffer, int off, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (off < 0)
                throw new ArgumentOutOfRangeException("offset", "< 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "< 0");
            int len = buffer.Length;
            if (off > len)
                throw new ArgumentException("destination offset is beyond array size");
            if (off > len - count)
                throw new ArgumentException("Reading would overrun buffer");

            if (this.remaining_body == 0)
                return -1;

            if (this.length == 0)
                return 0;

            int size = Math.Min(this.length, count);
            if (this.remaining_body > 0)
                size = (int)Math.Min(size, this.remaining_body);

            if (this.offset > this.buffer.Length - size)
            {
                size = Math.Min(size, this.buffer.Length - this.offset);
            }
            if (size == 0)
                return 0;

            Buffer.BlockCopy(this.buffer, this.offset, buffer, off, size);
            this.offset += size;
            this.length -= size;
            if (this.remaining_body > 0)
                remaining_body -= size;
            return size;
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (disposed)
                throw new ObjectDisposedException(typeof(RequestStream).ToString());

            // Call FillFromBuffer to check for buffer boundaries even when remaining_body is 0
            int nread = FillFromBuffer(buffer, offset, count);
            if (nread == -1)
            { // No more bytes available (Content-Length)
                return 0;
            }
            else if (nread > 0)
            {
                return nread;
            }

            nread = stream.Read(buffer, offset, count);
            if (nread > 0 && remaining_body > 0)
                remaining_body -= nread;
            return nread;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (disposed)
                throw new ObjectDisposedException(typeof(RequestStream).ToString());

            int nread = FillFromBuffer(buffer, offset, count);
            if (nread > 0 || nread == -1)
            {
                return Math.Max(0, nread);
            }

            // Avoid reading past the end of the request to allow
            // for HTTP pipelining
            if (remaining_body >= 0 && count > remaining_body)
                count = (int)Math.Min(Int32.MaxValue, remaining_body);

            nread = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            if (remaining_body > 0 && nread > 0)
                remaining_body -= nread;
            return nread;
        }

        //public override IAsyncResult BeginRead(byte[] buffer, int offset, int count,
        //                    AsyncCallback cback, object state)
        //{
        //    if (disposed)
        //        throw new ObjectDisposedException(typeof(RequestStream).ToString());

        //    int nread = FillFromBuffer(buffer, offset, count);
        //    if (nread > 0 || nread == -1)
        //    {
        //        HttpStreamAsyncResult ares = new HttpStreamAsyncResult();
        //        ares.Buffer = buffer;
        //        ares.Offset = offset;
        //        ares.Count = count;
        //        ares.Callback = cback;
        //        ares.State = state;
        //        ares.SynchRead = Math.Max(0, nread);
        //        ares.Complete();
        //        return ares;
        //    }

        //    // Avoid reading past the end of the request to allow
        //    // for HTTP pipelining
        //    if (remaining_body >= 0 && count > remaining_body)
        //        count = (int)Math.Min(Int32.MaxValue, remaining_body);
        //    return stream.BeginRead(buffer, offset, count, cback, state);
        //}

        //public override int EndRead(IAsyncResult ares)
        //{
        //    if (disposed)
        //        throw new ObjectDisposedException(typeof(RequestStream).ToString());

        //    if (ares == null)
        //        throw new ArgumentNullException("async_result");

        //    if (ares is HttpStreamAsyncResult)
        //    {
        //        HttpStreamAsyncResult r = (HttpStreamAsyncResult)ares;
        //        if (!ares.IsCompleted)
        //            ares.AsyncWaitHandle.WaitOne();
        //        return r.SynchRead;
        //    }

        //    // Close on exception?
        //    int nread = stream.EndRead(ares);
        //    if (remaining_body > 0 && nread > 0)
        //        remaining_body -= nread;
        //    return nread;
        //}

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        //public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count,
        //                    AsyncCallback cback, object state)
        //{
        //    throw new NotSupportedException();
        //}

        //public override void EndWrite(IAsyncResult async_result)
        //{
        //    throw new NotSupportedException();
        //}
    }
}
