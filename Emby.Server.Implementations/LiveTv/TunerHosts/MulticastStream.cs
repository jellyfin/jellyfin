using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class MulticastStream
    {
        private readonly ConcurrentDictionary<Guid,QueueStream> _outputStreams = new ConcurrentDictionary<Guid, QueueStream>();
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

            byte[] buffer = new byte[BufferSize];

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                if (bytesRead > 0)
                {
                    byte[] copy = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, copy, 0, bytesRead);

                    var allStreams = _outputStreams.ToList();
                    foreach (var stream in allStreams)
                    {
                        stream.Value.Queue(copy);
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

            _outputStreams.TryAdd(result.Id, result);

            result.Start(_cancellationToken);

            return result.TaskCompletion.Task;
        }

        public void RemoveOutputStream(QueueStream stream)
        {
            QueueStream removed;
            _outputStreams.TryRemove(stream.Id, out removed);
        }

        private void OnFinished(QueueStream queueStream)
        {
            RemoveOutputStream(queueStream);
        }
    }
}
