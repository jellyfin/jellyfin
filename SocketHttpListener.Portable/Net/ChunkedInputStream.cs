using System;
using System.IO;
using System.Runtime.InteropServices;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    class ChunkedInputStream : RequestStream
    {
        bool disposed;
        ChunkStream decoder;
        HttpListenerContext context;
        bool no_more_data;

        //class ReadBufferState
        //{
        //    public byte[] Buffer;
        //    public int Offset;
        //    public int Count;
        //    public int InitialCount;
        //    public HttpStreamAsyncResult Ares;
        //    public ReadBufferState(byte[] buffer, int offset, int count,
        //                HttpStreamAsyncResult ares)
        //    {
        //        Buffer = buffer;
        //        Offset = offset;
        //        Count = count;
        //        InitialCount = count;
        //        Ares = ares;
        //    }
        //}

        public ChunkedInputStream(HttpListenerContext context, Stream stream,
                        byte[] buffer, int offset, int length)
            : base(stream, buffer, offset, length)
        {
            this.context = context;
            WebHeaderCollection coll = (WebHeaderCollection)context.Request.Headers;
            decoder = new ChunkStream(coll);
        }

        //public ChunkStream Decoder
        //{
        //    get { return decoder; }
        //    set { decoder = value; }
        //}

        //public override int Read([In, Out] byte[] buffer, int offset, int count)
        //{
        //    IAsyncResult ares = BeginRead(buffer, offset, count, null, null);
        //    return EndRead(ares);
        //}

        //public override IAsyncResult BeginRead(byte[] buffer, int offset, int count,
        //                    AsyncCallback cback, object state)
        //{
        //    if (disposed)
        //        throw new ObjectDisposedException(GetType().ToString());

        //    if (buffer == null)
        //        throw new ArgumentNullException("buffer");

        //    int len = buffer.Length;
        //    if (offset < 0 || offset > len)
        //        throw new ArgumentOutOfRangeException("offset exceeds the size of buffer");

        //    if (count < 0 || offset > len - count)
        //        throw new ArgumentOutOfRangeException("offset+size exceeds the size of buffer");

        //    HttpStreamAsyncResult ares = new HttpStreamAsyncResult();
        //    ares.Callback = cback;
        //    ares.State = state;
        //    if (no_more_data)
        //    {
        //        ares.Complete();
        //        return ares;
        //    }
        //    int nread = decoder.Read(buffer, offset, count);
        //    offset += nread;
        //    count -= nread;
        //    if (count == 0)
        //    {
        //        // got all we wanted, no need to bother the decoder yet
        //        ares.Count = nread;
        //        ares.Complete();
        //        return ares;
        //    }
        //    if (!decoder.WantMore)
        //    {
        //        no_more_data = nread == 0;
        //        ares.Count = nread;
        //        ares.Complete();
        //        return ares;
        //    }
        //    ares.Buffer = new byte[8192];
        //    ares.Offset = 0;
        //    ares.Count = 8192;
        //    ReadBufferState rb = new ReadBufferState(buffer, offset, count, ares);
        //    rb.InitialCount += nread;
        //    base.BeginRead(ares.Buffer, ares.Offset, ares.Count, OnRead, rb);
        //    return ares;
        //}

        //void OnRead(IAsyncResult base_ares)
        //{
        //    ReadBufferState rb = (ReadBufferState)base_ares.AsyncState;
        //    HttpStreamAsyncResult ares = rb.Ares;
        //    try
        //    {
        //        int nread = base.EndRead(base_ares);
        //        decoder.Write(ares.Buffer, ares.Offset, nread);
        //        nread = decoder.Read(rb.Buffer, rb.Offset, rb.Count);
        //        rb.Offset += nread;
        //        rb.Count -= nread;
        //        if (rb.Count == 0 || !decoder.WantMore || nread == 0)
        //        {
        //            no_more_data = !decoder.WantMore && nread == 0;
        //            ares.Count = rb.InitialCount - rb.Count;
        //            ares.Complete();
        //            return;
        //        }
        //        ares.Offset = 0;
        //        ares.Count = Math.Min(8192, decoder.ChunkLeft + 6);
        //        base.BeginRead(ares.Buffer, ares.Offset, ares.Count, OnRead, rb);
        //    }
        //    catch (Exception e)
        //    {
        //        context.Connection.SendError(e.Message, 400);
        //        ares.Complete(e);
        //    }
        //}

        //public override int EndRead(IAsyncResult ares)
        //{
        //    if (disposed)
        //        throw new ObjectDisposedException(GetType().ToString());

        //    HttpStreamAsyncResult my_ares = ares as HttpStreamAsyncResult;
        //    if (ares == null)
        //        throw new ArgumentException("Invalid IAsyncResult", "ares");

        //    if (!ares.IsCompleted)
        //        ares.AsyncWaitHandle.WaitOne();

        //    if (my_ares.Error != null)
        //        throw new HttpListenerException(400, "I/O operation aborted: " + my_ares.Error.Message);

        //    return my_ares.Count;
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (!disposed)
        //    {
        //        disposed = true;
        //        base.Dispose(disposing);
        //    }
        //}
    }
}
