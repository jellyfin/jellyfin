using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace SocketHttpListener.Net
{
    class ChunkStream
    {
        enum State
        {
            None,
            PartialSize,
            Body,
            BodyFinished,
            Trailer
        }

        class Chunk
        {
            public byte[] Bytes;
            public int Offset;

            public Chunk(byte[] chunk)
            {
                this.Bytes = chunk;
            }

            public int Read(byte[] buffer, int offset, int size)
            {
                int nread = (size > Bytes.Length - Offset) ? Bytes.Length - Offset : size;
                Buffer.BlockCopy(Bytes, Offset, buffer, offset, nread);
                Offset += nread;
                return nread;
            }
        }

        internal WebHeaderCollection headers;
        int chunkSize;
        int chunkRead;
        int totalWritten;
        State state;
        //byte [] waitBuffer;
        StringBuilder saved;
        bool sawCR;
        bool gotit;
        int trailerState;
        List<Chunk> chunks;

        public ChunkStream(WebHeaderCollection headers)
        {
            this.headers = headers;
            saved = new StringBuilder();
            chunks = new List<Chunk>();
            chunkSize = -1;
            totalWritten = 0;
        }

        public void ResetBuffer()
        {
            chunkSize = -1;
            chunkRead = 0;
            totalWritten = 0;
            chunks.Clear();
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

        int ReadFromChunks(byte[] buffer, int offset, int size)
        {
            int count = chunks.Count;
            int nread = 0;

            var chunksForRemoving = new List<Chunk>(count);
            for (int i = 0; i < count; i++)
            {
                Chunk chunk = (Chunk)chunks[i];

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
                chunks.Remove(chunk);

            return nread;
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            if (offset < size)
                InternalWrite(buffer, ref offset, size);
        }

        void InternalWrite(byte[] buffer, ref int offset, int size)
        {
            if (state == State.None || state == State.PartialSize)
            {
                state = GetChunkSize(buffer, ref offset, size);
                if (state == State.PartialSize)
                    return;

                saved.Length = 0;
                sawCR = false;
                gotit = false;
            }

            if (state == State.Body && offset < size)
            {
                state = ReadBody(buffer, ref offset, size);
                if (state == State.Body)
                    return;
            }

            if (state == State.BodyFinished && offset < size)
            {
                state = ReadCRLF(buffer, ref offset, size);
                if (state == State.BodyFinished)
                    return;

                sawCR = false;
            }

            if (state == State.Trailer && offset < size)
            {
                state = ReadTrailer(buffer, ref offset, size);
                if (state == State.Trailer)
                    return;

                saved.Length = 0;
                sawCR = false;
                gotit = false;
            }

            if (offset < size)
                InternalWrite(buffer, ref offset, size);
        }

        public bool WantMore
        {
            get { return (chunkRead != chunkSize || chunkSize != 0 || state != State.None); }
        }

        public bool DataAvailable
        {
            get
            {
                int count = chunks.Count;
                for (int i = 0; i < count; i++)
                {
                    Chunk ch = (Chunk)chunks[i];
                    if (ch == null || ch.Bytes == null)
                        continue;
                    if (ch.Bytes.Length > 0 && ch.Offset < ch.Bytes.Length)
                        return (state != State.Body);
                }
                return false;
            }
        }

        public int TotalDataSize
        {
            get { return totalWritten; }
        }

        public int ChunkLeft
        {
            get { return chunkSize - chunkRead; }
        }

        State ReadBody(byte[] buffer, ref int offset, int size)
        {
            if (chunkSize == 0)
                return State.BodyFinished;

            int diff = size - offset;
            if (diff + chunkRead > chunkSize)
                diff = chunkSize - chunkRead;

            byte[] chunk = new byte[diff];
            Buffer.BlockCopy(buffer, offset, chunk, 0, diff);
            chunks.Add(new Chunk(chunk));
            offset += diff;
            chunkRead += diff;
            totalWritten += diff;
            return (chunkRead == chunkSize) ? State.BodyFinished : State.Body;

        }

        State GetChunkSize(byte[] buffer, ref int offset, int size)
        {
            chunkRead = 0;
            chunkSize = 0;
            char c = '\0';
            while (offset < size)
            {
                c = (char)buffer[offset++];
                if (c == '\r')
                {
                    if (sawCR)
                        ThrowProtocolViolation("2 CR found");

                    sawCR = true;
                    continue;
                }

                if (sawCR && c == '\n')
                    break;

                if (c == ' ')
                    gotit = true;

                if (!gotit)
                    saved.Append(c);

                if (saved.Length > 20)
                    ThrowProtocolViolation("chunk size too long.");
            }

            if (!sawCR || c != '\n')
            {
                if (offset < size)
                    ThrowProtocolViolation("Missing \\n");

                try
                {
                    if (saved.Length > 0)
                    {
                        chunkSize = Int32.Parse(RemoveChunkExtension(saved.ToString()), NumberStyles.HexNumber);
                    }
                }
                catch (Exception)
                {
                    ThrowProtocolViolation("Cannot parse chunk size.");
                }

                return State.PartialSize;
            }

            chunkRead = 0;
            try
            {
                chunkSize = Int32.Parse(RemoveChunkExtension(saved.ToString()), NumberStyles.HexNumber);
            }
            catch (Exception)
            {
                ThrowProtocolViolation("Cannot parse chunk size.");
            }

            if (chunkSize == 0)
            {
                trailerState = 2;
                return State.Trailer;
            }

            return State.Body;
        }

        static string RemoveChunkExtension(string input)
        {
            int idx = input.IndexOf(';');
            if (idx == -1)
                return input;
            return input.Substring(0, idx);
        }

        State ReadCRLF(byte[] buffer, ref int offset, int size)
        {
            if (!sawCR)
            {
                if ((char)buffer[offset++] != '\r')
                    ThrowProtocolViolation("Expecting \\r");

                sawCR = true;
                if (offset == size)
                    return State.BodyFinished;
            }

            if (sawCR && (char)buffer[offset++] != '\n')
                ThrowProtocolViolation("Expecting \\n");

            return State.None;
        }

        State ReadTrailer(byte[] buffer, ref int offset, int size)
        {
            char c = '\0';

            // short path
            if (trailerState == 2 && (char)buffer[offset] == '\r' && saved.Length == 0)
            {
                offset++;
                if (offset < size && (char)buffer[offset] == '\n')
                {
                    offset++;
                    return State.None;
                }
                offset--;
            }

            int st = trailerState;
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
                    saved.Append(stString.Substring(0, saved.Length == 0 ? st - 2 : st));
                    st = 0;
                    if (saved.Length > 4196)
                        ThrowProtocolViolation("Error reading trailer (too long).");
                }
            }

            if (st < 4)
            {
                trailerState = st;
                if (offset < size)
                    ThrowProtocolViolation("Error reading trailer.");

                return State.Trailer;
            }

            StringReader reader = new StringReader(saved.ToString());
            string line;
            while ((line = reader.ReadLine()) != null && line != "")
                headers.Add(line);

            return State.None;
        }

        static void ThrowProtocolViolation(string message)
        {
            WebException we = new WebException(message, null, WebExceptionStatus.UnknownError, null);
            //WebException we = new WebException(message, null, WebExceptionStatus.ServerProtocolViolation, null);
            throw we;
        }
    }
}
