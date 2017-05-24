using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

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

    internal sealed class ChunkStream
    {
        private enum State
        {
            None,
            PartialSize,
            Body,
            BodyFinished,
            Trailer
        }

        private class Chunk
        {
            public byte[] Bytes;
            public int Offset;

            public Chunk(byte[] chunk)
            {
                Bytes = chunk;
            }

            public int Read(byte[] buffer, int offset, int size)
            {
                int nread = (size > Bytes.Length - Offset) ? Bytes.Length - Offset : size;
                Buffer.BlockCopy(Bytes, Offset, buffer, offset, nread);
                Offset += nread;
                return nread;
            }
        }

        internal WebHeaderCollection _headers;
        private int _chunkSize;
        private int _chunkRead;
        private int _totalWritten;
        private State _state;
        private StringBuilder _saved;
        private bool _sawCR;
        private bool _gotit;
        private int _trailerState;
        private List<Chunk> _chunks;

        public ChunkStream(byte[] buffer, int offset, int size, WebHeaderCollection headers)
                    : this(headers)
        {
            Write(buffer, offset, size);
        }

        public ChunkStream(WebHeaderCollection headers)
        {
            _headers = headers;
            _saved = new StringBuilder();
            _chunks = new List<Chunk>();
            _chunkSize = -1;
            _totalWritten = 0;
        }

        public void ResetBuffer()
        {
            _chunkSize = -1;
            _chunkRead = 0;
            _totalWritten = 0;
            _chunks.Clear();
        }

        public void WriteAndReadBack(byte[] buffer, int offset, int size, ref int read)
        {
            if (offset + read > 0)
                Write(buffer, offset, offset + read);
            read = Read(buffer, offset, size);
        }

        public int Read(byte[] buffer, int offset, int size)
        {
            return ReadFromChunks(buffer, offset, size);
        }

        private int ReadFromChunks(byte[] buffer, int offset, int size)
        {
            int count = _chunks.Count;
            int nread = 0;

            var chunksForRemoving = new List<Chunk>(count);
            for (int i = 0; i < count; i++)
            {
                Chunk chunk = _chunks[i];

                if (chunk.Offset == chunk.Bytes.Length)
                {
                    chunksForRemoving.Add(chunk);
                    continue;
                }

                nread += chunk.Read(buffer, offset + nread, size - nread);
                if (nread == size)
                    break;
            }

            foreach (var chunk in chunksForRemoving)
                _chunks.Remove(chunk);

            return nread;
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            if (offset < size)
                InternalWrite(buffer, ref offset, size);
        }

        private void InternalWrite(byte[] buffer, ref int offset, int size)
        {
            if (_state == State.None || _state == State.PartialSize)
            {
                _state = GetChunkSize(buffer, ref offset, size);
                if (_state == State.PartialSize)
                    return;

                _saved.Length = 0;
                _sawCR = false;
                _gotit = false;
            }

            if (_state == State.Body && offset < size)
            {
                _state = ReadBody(buffer, ref offset, size);
                if (_state == State.Body)
                    return;
            }

            if (_state == State.BodyFinished && offset < size)
            {
                _state = ReadCRLF(buffer, ref offset, size);
                if (_state == State.BodyFinished)
                    return;

                _sawCR = false;
            }

            if (_state == State.Trailer && offset < size)
            {
                _state = ReadTrailer(buffer, ref offset, size);
                if (_state == State.Trailer)
                    return;

                _saved.Length = 0;
                _sawCR = false;
                _gotit = false;
            }

            if (offset < size)
                InternalWrite(buffer, ref offset, size);
        }

        public bool WantMore
        {
            get { return (_chunkRead != _chunkSize || _chunkSize != 0 || _state != State.None); }
        }

        public bool DataAvailable
        {
            get
            {
                int count = _chunks.Count;
                for (int i = 0; i < count; i++)
                {
                    Chunk ch = _chunks[i];
                    if (ch == null || ch.Bytes == null)
                        continue;
                    if (ch.Bytes.Length > 0 && ch.Offset < ch.Bytes.Length)
                        return (_state != State.Body);
                }
                return false;
            }
        }

        public int TotalDataSize
        {
            get { return _totalWritten; }
        }

        public int ChunkLeft
        {
            get { return _chunkSize - _chunkRead; }
        }

        private State ReadBody(byte[] buffer, ref int offset, int size)
        {
            if (_chunkSize == 0)
                return State.BodyFinished;

            int diff = size - offset;
            if (diff + _chunkRead > _chunkSize)
                diff = _chunkSize - _chunkRead;

            byte[] chunk = new byte[diff];
            Buffer.BlockCopy(buffer, offset, chunk, 0, diff);
            _chunks.Add(new Chunk(chunk));
            offset += diff;
            _chunkRead += diff;
            _totalWritten += diff;

            return (_chunkRead == _chunkSize) ? State.BodyFinished : State.Body;
        }

        private State GetChunkSize(byte[] buffer, ref int offset, int size)
        {
            _chunkRead = 0;
            _chunkSize = 0;
            char c = '\0';
            while (offset < size)
            {
                c = (char)buffer[offset++];
                if (c == '\r')
                {
                    if (_sawCR)
                        ThrowProtocolViolation("2 CR found");

                    _sawCR = true;
                    continue;
                }

                if (_sawCR && c == '\n')
                    break;

                if (c == ' ')
                    _gotit = true;

                if (!_gotit)
                    _saved.Append(c);

                if (_saved.Length > 20)
                    ThrowProtocolViolation("chunk size too long.");
            }

            if (!_sawCR || c != '\n')
            {
                if (offset < size)
                    ThrowProtocolViolation("Missing \\n");

                try
                {
                    if (_saved.Length > 0)
                    {
                        _chunkSize = Int32.Parse(RemoveChunkExtension(_saved.ToString()), NumberStyles.HexNumber);
                    }
                }
                catch (Exception)
                {
                    ThrowProtocolViolation("Cannot parse chunk size.");
                }

                return State.PartialSize;
            }

            _chunkRead = 0;
            try
            {
                _chunkSize = Int32.Parse(RemoveChunkExtension(_saved.ToString()), NumberStyles.HexNumber);
            }
            catch (Exception)
            {
                ThrowProtocolViolation("Cannot parse chunk size.");
            }

            if (_chunkSize == 0)
            {
                _trailerState = 2;
                return State.Trailer;
            }

            return State.Body;
        }

        private static string RemoveChunkExtension(string input)
        {
            int idx = input.IndexOf(';');
            if (idx == -1)
                return input;
            return input.Substring(0, idx);
        }

        private State ReadCRLF(byte[] buffer, ref int offset, int size)
        {
            if (!_sawCR)
            {
                if ((char)buffer[offset++] != '\r')
                    ThrowProtocolViolation("Expecting \\r");

                _sawCR = true;
                if (offset == size)
                    return State.BodyFinished;
            }

            if (_sawCR && (char)buffer[offset++] != '\n')
                ThrowProtocolViolation("Expecting \\n");

            return State.None;
        }

        private State ReadTrailer(byte[] buffer, ref int offset, int size)
        {
            char c = '\0';

            // short path
            if (_trailerState == 2 && (char)buffer[offset] == '\r' && _saved.Length == 0)
            {
                offset++;
                if (offset < size && (char)buffer[offset] == '\n')
                {
                    offset++;
                    return State.None;
                }
                offset--;
            }

            int st = _trailerState;
            string stString = "\r\n\r";
            while (offset < size && st < 4)
            {
                c = (char)buffer[offset++];
                if ((st == 0 || st == 2) && c == '\r')
                {
                    st++;
                    continue;
                }

                if ((st == 1 || st == 3) && c == '\n')
                {
                    st++;
                    continue;
                }

                if (st > 0)
                {
                    _saved.Append(stString.Substring(0, _saved.Length == 0 ? st - 2 : st));
                    st = 0;
                    if (_saved.Length > 4196)
                        ThrowProtocolViolation("Error reading trailer (too long).");
                }
            }

            if (st < 4)
            {
                _trailerState = st;
                if (offset < size)
                    ThrowProtocolViolation("Error reading trailer.");

                return State.Trailer;
            }

            StringReader reader = new StringReader(_saved.ToString());
            string line;
            while ((line = reader.ReadLine()) != null && line != "")
                _headers.Add(line);

            return State.None;
        }

        private static void ThrowProtocolViolation(string message)
        {
            WebException we = new WebException(message, null, WebExceptionStatus.ServerProtocolViolation, null);
            throw we;
        }
    }
}
