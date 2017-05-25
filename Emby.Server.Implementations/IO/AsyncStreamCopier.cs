using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Emby.Server.Implementations.IO
{
    public class AsyncStreamCopier : IDisposable
    {
        // size in bytes of the buffers in the buffer pool
        private const int DefaultBufferSize = 4096;
        private readonly int _bufferSize;
        // number of buffers in the pool
        private const int DefaultBufferCount = 4;
        private readonly int _bufferCount;

        // indexes of the next buffer to read into/write from
        private int _nextReadBuffer = -1;
        private int _nextWriteBuffer = -1;

        // the buffer pool, implemented as an array, and used in a cyclic way
        private readonly byte[][] _buffers;
        // sizes in bytes of the available (read) data in the buffers
        private readonly int[] _sizes;
        // the streams...
        private Stream _source;
        private Stream _target;
        private readonly bool _closeStreamsOnEnd;

        // number of buffers that are ready to be written
        private int _buffersToWrite;
        // flag indicating that there is still a read operation to be scheduled
        // (source end of stream not reached)
        private volatile bool _moreDataToRead;
        // the result of the whole operation, returned by BeginCopy()
        private AsyncResult _asyncResult;
        // any exception that occurs during an async operation
        // stored here for rethrow
        private Exception _exception;

        public TaskCompletionSource<bool> TaskCompletionSource;
        private long _bytesToRead;
        private long _totalBytesWritten;
        private CancellationToken _cancellationToken;

        public AsyncStreamCopier(Stream source,
                                 Stream target,
                                 long bytesToRead, 
                                 CancellationToken cancellationToken, 
                                 bool closeStreamsOnEnd = false,
                                 int bufferSize = DefaultBufferSize,
                                 int bufferCount = DefaultBufferCount)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (target == null)
                throw new ArgumentNullException("target");
            if (!source.CanRead)
                throw new ArgumentException("Cannot copy from a non-readable stream.");
            if (!target.CanWrite)
                throw new ArgumentException("Cannot copy to a non-writable stream.");
            _source = source;
            _target = target;
            _moreDataToRead = true;
            _closeStreamsOnEnd = closeStreamsOnEnd;
            _bufferSize = bufferSize;
            _bufferCount = bufferCount;
            _buffers = new byte[_bufferCount][];
            _sizes = new int[_bufferCount];
            _bytesToRead = bytesToRead;
            _cancellationToken = cancellationToken;
        }

        ~AsyncStreamCopier()
        {
            // ensure any exception cannot be ignored
            ThrowExceptionIfNeeded();
        }

        public static Task CopyStream(Stream source, Stream target, int bufferSize, int bufferCount, CancellationToken cancellationToken)
        {
            return CopyStream(source, target, 0, bufferSize, bufferCount, cancellationToken);
        }

        public static Task CopyStream(Stream source, Stream target, long size, int bufferSize, int bufferCount, CancellationToken cancellationToken)
        {
            var copier = new AsyncStreamCopier(source, target, size, cancellationToken, false, bufferSize, bufferCount);
            var taskCompletion = new TaskCompletionSource<bool>();

            copier.TaskCompletionSource = taskCompletion;

            var result = copier.BeginCopy(StreamCopyCallback, copier);

            if (result.CompletedSynchronously)
            {
                StreamCopyCallback(result);
            }

            cancellationToken.Register(() => taskCompletion.TrySetCanceled());

            return taskCompletion.Task;
        }

        private static void StreamCopyCallback(IAsyncResult result)
        {
            var copier = (AsyncStreamCopier)result.AsyncState;
            var taskCompletion = copier.TaskCompletionSource;

            try
            {
                copier.EndCopy(result);
                taskCompletion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                taskCompletion.TrySetException(ex);
            }
        }

        public void Dispose()
        {
            if (_asyncResult != null)
                _asyncResult.Dispose();
            if (_closeStreamsOnEnd)
            {
                if (_source != null)
                {
                    _source.Dispose();
                    _source = null;
                }
                if (_target != null)
                {
                    _target.Dispose();
                    _target = null;
                }
            }
            GC.SuppressFinalize(this);
            ThrowExceptionIfNeeded();
        }

        public IAsyncResult BeginCopy(AsyncCallback callback, object state)
        {
            // avoid concurrent start of the copy on separate threads
            if (Interlocked.CompareExchange(ref _asyncResult, new AsyncResult(callback, state), null) != null)
                throw new InvalidOperationException("A copy operation has already been started on this object.");
            // allocate buffers
            for (int i = 0; i < _bufferCount; i++)
                _buffers[i] = new byte[_bufferSize];

            // we pass false to BeginRead() to avoid completing the async result
            // immediately which would result in invoking the callback
            // when the method fails synchronously
            BeginRead(false);
            // throw exception synchronously if there is one
            ThrowExceptionIfNeeded();
            return _asyncResult;
        }

        public void EndCopy(IAsyncResult ar)
        {
            if (ar != _asyncResult)
                throw new InvalidOperationException("Invalid IAsyncResult object.");

            if (!_asyncResult.IsCompleted)
                _asyncResult.AsyncWaitHandle.WaitOne();

            if (_closeStreamsOnEnd)
            {
                _source.Close();
                _source = null;
                _target.Close();
                _target = null;
            }

            //_logger.Info("AsyncStreamCopier {0} bytes requested. {1} bytes transferred", _bytesToRead, _totalBytesWritten);
            ThrowExceptionIfNeeded();
        }

        /// <summary>
        /// Here we'll throw a pending exception if there is one, 
        /// and remove it from our instance, so we know it has been consumed.
        /// </summary>
        private void ThrowExceptionIfNeeded()
        {
            if (_exception != null)
            {
                var exception = _exception;
                _exception = null;
                throw exception;
            }
        }

        private void BeginRead(bool completeOnError = true)
        {
            if (!_moreDataToRead)
            {
                return;
            }
            if (_asyncResult.IsCompleted)
                return;
            int bufferIndex = Interlocked.Increment(ref _nextReadBuffer) % _bufferCount;

            try
            {
                _source.BeginRead(_buffers[bufferIndex], 0, _bufferSize, EndRead, bufferIndex);
            }
            catch (Exception exception)
            {
                _exception = exception;
                if (completeOnError)
                    _asyncResult.Complete(false);
            }
        }

        private void BeginWrite()
        {
            if (_asyncResult.IsCompleted)
                return;
            // this method can actually be called concurrently!!
            // indeed, let's say we call a BeginWrite, and the thread gets interrupted 
            // just after making the IO request.
            // At that moment, the thread is still in the method. And then the IO request
            // ends (extremely fast io, or caching...), EndWrite gets called
            // on another thread, and calls BeginWrite again! There we have it!
            // That is the reason why an Interlocked is needed here.
            int bufferIndex = Interlocked.Increment(ref _nextWriteBuffer) % _bufferCount;

            try
            {
                int bytesToWrite;
                if (_bytesToRead > 0)
                {
                    var bytesLeftToWrite = _bytesToRead - _totalBytesWritten;
                    bytesToWrite = Convert.ToInt32(Math.Min(_sizes[bufferIndex], bytesLeftToWrite));
                }
                else
                {
                    bytesToWrite = _sizes[bufferIndex];
                }

                _target.BeginWrite(_buffers[bufferIndex], 0, bytesToWrite, EndWrite, null);

                _totalBytesWritten += bytesToWrite;
            }
            catch (Exception exception)
            {
                _exception = exception;
                _asyncResult.Complete(false);
            }
        }

        private void EndRead(IAsyncResult ar)
        {
            try
            {
                int read = _source.EndRead(ar);
                _moreDataToRead = read > 0;
                var bufferIndex = (int)ar.AsyncState;
                _sizes[bufferIndex] = read;
            }
            catch (Exception exception)
            {
                _exception = exception;
                _asyncResult.Complete(false);
                return;
            }

            if (_moreDataToRead && !_cancellationToken.IsCancellationRequested)
            {
                int usedBuffers = Interlocked.Increment(ref _buffersToWrite);
                // if we incremented from zero to one, then it means we just 
                // added the single buffer to write, so a writer could not 
                // be busy, and we have to schedule one.
                if (usedBuffers == 1)
                    BeginWrite();
                // test if there is at least a free buffer, and schedule
                // a read, as we have read some data
                if (usedBuffers < _bufferCount)
                    BeginRead();
            }
            else
            {
                // we did not add a buffer, because no data was read, and 
                // there is no buffer left to write so this is the end...
                if (Thread.VolatileRead(ref _buffersToWrite) == 0)
                {
                    _asyncResult.Complete(false);
                }
            }
        }

        private void EndWrite(IAsyncResult ar)
        {
            try
            {
                _target.EndWrite(ar);
            }
            catch (Exception exception)
            {
                _exception = exception;
                _asyncResult.Complete(false);
                return;
            }

            int buffersLeftToWrite = Interlocked.Decrement(ref _buffersToWrite);
            // no reader could be active if all buffers were full of data waiting to be written
            bool noReaderIsBusy = buffersLeftToWrite == _bufferCount - 1;
            // note that it is possible that both a reader and
            // a writer see the end of the copy and call Complete
            // on the _asyncResult object. That race condition is handled by
            // Complete that ensures it is only executed fully once.

            long bytesLeftToWrite;
            if (_bytesToRead > 0)
            {
                bytesLeftToWrite = _bytesToRead - _totalBytesWritten;
            }
            else
            {
                bytesLeftToWrite = 1;
            }

            if (!_moreDataToRead || bytesLeftToWrite <= 0 || _cancellationToken.IsCancellationRequested)
            {
                // at this point we know no reader can schedule a read or write
                if (Thread.VolatileRead(ref _buffersToWrite) == 0)
                {
                    // nothing left to write, so it is the end
                    _asyncResult.Complete(false);
                    return;
                }
            }
            else
                // here, we know we have something left to read, 
                // so schedule a read if no read is busy
                if (noReaderIsBusy)
                BeginRead();

            // also schedule a write if we are sure we did not write the last buffer
            // note that if buffersLeftToWrite is zero and a reader has put another
            // buffer to write between the time we decremented _buffersToWrite 
            // and now, that reader will also schedule another write,
            // as it will increment _buffersToWrite from zero to one
            if (buffersLeftToWrite > 0)
                BeginWrite();
        }
    }

    internal class AsyncResult : IAsyncResult, IDisposable
    {
        // Fields set at construction which never change while
        // operation is pending
        private readonly AsyncCallback _asyncCallback;
        private readonly object _asyncState;

        // Fields set at construction which do change after
        // operation completes
        private const int StatePending = 0;
        private const int StateCompletedSynchronously = 1;
        private const int StateCompletedAsynchronously = 2;
        private int _completedState = StatePending;

        // Field that may or may not get set depending on usage
        private ManualResetEvent _waitHandle;

        internal AsyncResult(
            AsyncCallback asyncCallback,
            object state)
        {
            _asyncCallback = asyncCallback;
            _asyncState = state;
        }

        internal bool Complete(bool completedSynchronously)
        {
            bool result = false;

            // The _completedState field MUST be set prior calling the callback
            int prevState = Interlocked.CompareExchange(ref _completedState,
                completedSynchronously ? StateCompletedSynchronously :
                StateCompletedAsynchronously, StatePending);
            if (prevState == StatePending)
            {
                // If the event exists, set it
                if (_waitHandle != null)
                    _waitHandle.Set();

                if (_asyncCallback != null)
                    _asyncCallback(this);

                result = true;
            }

            return result;
        }

        #region Implementation of IAsyncResult

        public Object AsyncState { get { return _asyncState; } }

        public bool CompletedSynchronously
        {
            get
            {
                return Thread.VolatileRead(ref _completedState) ==
                    StateCompletedSynchronously;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_waitHandle == null)
                {
                    bool done = IsCompleted;
                    var mre = new ManualResetEvent(done);
                    if (Interlocked.CompareExchange(ref _waitHandle,
                        mre, null) != null)
                    {
                        // Another thread created this object's event; dispose
                        // the event we just created
                        mre.Close();
                    }
                    else
                    {
                        if (!done && IsCompleted)
                        {
                            // If the operation wasn't done when we created
                            // the event but now it is done, set the event
                            _waitHandle.Set();
                        }
                    }
                }
                return _waitHandle;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Thread.VolatileRead(ref _completedState) !=
                    StatePending;
            }
        }
        #endregion

        public void Dispose()
        {
            if (_waitHandle != null)
            {
                _waitHandle.Dispose();
                _waitHandle = null;
            }
        }
    }
}
