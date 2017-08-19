using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class MulticastStream
    {
        private readonly ConcurrentDictionary<Guid, QueueStream> _outputStreams = new ConcurrentDictionary<Guid, QueueStream>();
        private const int BufferSize = 81920;
        private readonly ILogger _logger;

        public MulticastStream(ILogger logger)
        {
            _logger = logger;
        }

        public async Task CopyUntilCancelled(Stream source, Action onStarted, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[] buffer = new byte[BufferSize];

                var bytesRead = source.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    foreach (var stream in _outputStreams)
                    {
                        stream.Value.Queue(buffer, 0, bytesRead);
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

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            var queueStream = new QueueStream(stream, _logger);

            _outputStreams.TryAdd(queueStream.Id, queueStream);

            try
            {
                queueStream.Start(cancellationToken);
            }
            finally
            {
                _outputStreams.TryRemove(queueStream.Id, out queueStream);
                GC.Collect();
            }

            return Task.FromResult(true);
        }
    }
}
