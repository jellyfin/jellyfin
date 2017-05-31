using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketHttpListener.Net
{
    internal class HttpStreamAsyncResult : IAsyncResult
    {
        private object _locker = new object();
        private ManualResetEvent _handle;
        private bool _completed;

        internal readonly object _parent;
        internal byte[] _buffer;
        internal int _offset;
        internal int _count;
        internal AsyncCallback _callback;
        internal object _state;
        internal int _synchRead;
        internal Exception _error;
        internal bool _endCalled;

        internal HttpStreamAsyncResult(object parent)
        {
            _parent = parent;
        }

        public void Complete(Exception e)
        {
            _error = e;
            Complete();
        }

        public void Complete()
        {
            lock (_locker)
            {
                if (_completed)
                    return;

                _completed = true;
                if (_handle != null)
                    _handle.Set();

                if (_callback != null)
                    Task.Run(() => _callback(this));
            }
        }

        public object AsyncState
        {
            get { return _state; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (_locker)
                {
                    if (_handle == null)
                        _handle = new ManualResetEvent(_completed);
                }

                return _handle;
            }
        }

        public bool CompletedSynchronously
        {
            get { return (_synchRead == _count); }
        }

        public bool IsCompleted
        {
            get
            {
                lock (_locker)
                {
                    return _completed;
                }
            }
        }
    }
}
