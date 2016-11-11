using System;
using System.Threading;

namespace SocketHttpListener.Net
{
    class HttpStreamAsyncResult : IAsyncResult
    {
        object locker = new object();
        ManualResetEvent handle;
        bool completed;

        internal byte[] Buffer;
        internal int Offset;
        internal int Count;
        internal AsyncCallback Callback;
        internal object State;
        internal int SynchRead;
        internal Exception Error;

        public void Complete(Exception e)
        {
            Error = e;
            Complete();
        }

        public void Complete()
        {
            lock (locker)
            {
                if (completed)
                    return;

                completed = true;
                if (handle != null)
                    handle.Set();

                if (Callback != null)
                    Callback.BeginInvoke(this, null, null);
            }
        }

        public object AsyncState
        {
            get { return State; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (locker)
                {
                    if (handle == null)
                        handle = new ManualResetEvent(completed);
                }

                return handle;
            }
        }

        public bool CompletedSynchronously
        {
            get { return (SynchRead == Count); }
        }

        public bool IsCompleted
        {
            get
            {
                lock (locker)
                {
                    return completed;
                }
            }
        }
    }
}
