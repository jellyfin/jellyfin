using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts
{
    public class MulticastStream
    {
        private readonly List<QueueStream> _outputStreams = new List<QueueStream>();
        private const int BufferSize = 81920;
        private CancellationToken _cancellationToken;
        private readonly ILogger _logger;

        public MulticastStream(ILogger logger)
        {
            _logger = logger;
        }

        public async Task CopyUntilCancelled(Stream source, Action onStarted, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] buffer = new byte[BufferSize];

                var bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                if (bytesRead > 0)
                {
                    byte[] copy = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, copy, 0, bytesRead);
                   
                    List<QueueStream> streams = null;

                    lock (_outputStreams)
                    {
                        streams = _outputStreams.ToList();
                    }

                    foreach (var stream in streams)
                    {
                        stream.Queue(copy);
                    }

                    if (onStarted != null)
                    {
                        var onStartedCopy = onStarted;
                        onStarted = null;
                        Task.Run(onStartedCopy);
                    }
                }

                else
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        public Task CopyToAsync(Stream stream)
        {
            var result = new QueueStream(stream, _logger)
            {
                OnFinished = OnFinished
            };

            lock (_outputStreams)
            {
                _outputStreams.Add(result);
            }

            result.Start(_cancellationToken);

            return result.TaskCompletion.Task;
        }

        public void RemoveOutputStream(QueueStream stream)
        {
            lock (_outputStreams)
            {
                _outputStreams.Remove(stream);
            }
        }

        private void OnFinished(QueueStream queueStream)
        {
            RemoveOutputStream(queueStream);
        }
    }
}
