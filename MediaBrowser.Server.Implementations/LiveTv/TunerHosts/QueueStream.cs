using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts
{
    public class QueueStream
    {
        private readonly Stream _outputStream;
        private readonly ConcurrentQueue<byte[]> _queue = new ConcurrentQueue<byte[]>();
        private CancellationToken _cancellationToken;
        public TaskCompletionSource<bool> TaskCompletion { get; private set; }

        public Action<QueueStream> OnFinished { get; set; }
        private readonly ILogger _logger;

        public QueueStream(Stream outputStream, ILogger logger)
        {
            _outputStream = outputStream;
            _logger = logger;
            TaskCompletion = new TaskCompletionSource<bool>();
        }

        public void Queue(byte[] bytes)
        {
            _queue.Enqueue(bytes);
        }

        public void Start(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            Task.Run(() => StartInternal());
        }

        private byte[] Dequeue()
        {
            byte[] bytes;
            if (_queue.TryDequeue(out bytes))
            {
                return bytes;
            }

            return null;
        }

        private async Task StartInternal()
        {
            var cancellationToken = _cancellationToken;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var bytes = Dequeue();
                    if (bytes != null)
                    {
                        await _outputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                    }
                }

                TaskCompletion.TrySetResult(true);
                _logger.Debug("QueueStream complete");
            }
            catch (OperationCanceledException)
            {
                _logger.Debug("QueueStream cancelled");
                TaskCompletion.TrySetCanceled();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in QueueStream", ex);
                TaskCompletion.TrySetException(ex);
            }
            finally
            {
                if (OnFinished != null)
                {
                    OnFinished(this);
                }
            }
        }
    }
}
