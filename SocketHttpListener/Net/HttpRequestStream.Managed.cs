using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

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

    internal partial class HttpRequestStream : Stream
    {
        private byte[] _buffer;
        private int _offset;
        private int _length;
        private long _remainingBody;
        protected bool _closed;
        private Stream _stream;

        internal HttpRequestStream(Stream stream, byte[] buffer, int offset, int length)
            : this(stream, buffer, offset, length, -1)
        {
        }

        internal HttpRequestStream(Stream stream, byte[] buffer, int offset, int length, long contentlength)
        {
            _stream = stream;
            _buffer = buffer;
            _offset = offset;
            _length = length;
            _remainingBody = contentlength;
        }

        // Returns 0 if we can keep reading from the base stream,
        // > 0 if we read something from the buffer.
        // -1 if we had a content length set and we finished reading that many bytes.
        private int FillFromBuffer(byte[] buffer, int offset, int count)
        {
            if (_remainingBody == 0)
                return -1;

            if (_length == 0)
                return 0;

            int size = Math.Min(_length, count);
            if (_remainingBody > 0)
                size = (int)Math.Min(size, _remainingBody);

            if (_offset > _buffer.Length - size)
            {
                size = Math.Min(size, _buffer.Length - _offset);
            }
            if (size == 0)
                return 0;

            Buffer.BlockCopy(_buffer, _offset, buffer, offset, size);
            _offset += size;
            _length -= size;
            if (_remainingBody > 0)
                _remainingBody -= size;
            return size;
        }

        protected virtual int ReadCore(byte[] buffer, int offset, int size)
        {
            // Call FillFromBuffer to check for buffer boundaries even when remaining_body is 0
            int nread = FillFromBuffer(buffer, offset, size);
            if (nread == -1)
            { // No more bytes available (Content-Length)
                return 0;
            }
            else if (nread > 0)
            {
                return nread;
            }

            nread = _stream.Read(buffer, offset, size);
            if (nread > 0 && _remainingBody > 0)
                _remainingBody -= nread;
            return nread;
        }

        protected virtual IAsyncResult BeginReadCore(byte[] buffer, int offset, int size, AsyncCallback cback, object state)
        {
            if (size == 0 || _closed)
            {
                HttpStreamAsyncResult ares = new HttpStreamAsyncResult(this);
                ares._callback = cback;
                ares._state = state;
                ares.Complete();
                return ares;
            }

            int nread = FillFromBuffer(buffer, offset, size);
            if (nread > 0 || nread == -1)
            {
                HttpStreamAsyncResult ares = new HttpStreamAsyncResult(this);
                ares._buffer = buffer;
                ares._offset = offset;
                ares._count = size;
                ares._callback = cback;
                ares._state = state;
                ares._synchRead = Math.Max(0, nread);
                ares.Complete();
                return ares;
            }

            // Avoid reading past the end of the request to allow
            // for HTTP pipelining
            if (_remainingBody >= 0 && size > _remainingBody)
            {
                size = (int)Math.Min(int.MaxValue, _remainingBody);
            }

            return _stream.BeginRead(buffer, offset, size, cback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            var r = asyncResult as HttpStreamAsyncResult;

            if (r != null)
            {
                if (!ReferenceEquals(this, r._parent))
                {
                    throw new ArgumentException("Invalid async result");
                }
                if (r._endCalled)
                {
                    throw new InvalidOperationException("Invalid end call");
                }
                r._endCalled = true;

                if (!asyncResult.IsCompleted)
                {
                    asyncResult.AsyncWaitHandle.WaitOne();
                }

                return r._synchRead;
            }

            if (_closed)
                return 0;

            int nread = 0;
            try
            {
                nread = _stream.EndRead(asyncResult);
            }
            catch (IOException e) when (e.InnerException is ArgumentException || e.InnerException is InvalidOperationException)
            {
                throw e.InnerException;
            }

            if (_remainingBody > 0 && nread > 0)
            {
                _remainingBody -= nread;
            }

            return nread;
        }
    }
}
