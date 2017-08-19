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
    public class QueueStream
    {
        private readonly Stream _outputStream;
        private readonly BlockingCollection<Tuple<byte[], int, int>> _queue = new BlockingCollection<Tuple<byte[], int, int>>();

        private readonly ILogger _logger;
        public Guid Id = Guid.NewGuid();

        public QueueStream(Stream outputStream, ILogger logger)
        {
            _outputStream = outputStream;
            _logger = logger;
        }

        public void Queue(byte[] bytes, int offset, int count)
        {
            _queue.Add(new Tuple<byte[], int, int>(bytes, offset, count));
        }

        public void Start(CancellationToken cancellationToken)
        {
            while (true)
            {
                foreach (var result in _queue.GetConsumingEnumerable())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _outputStream.Write(result.Item1, result.Item2, result.Item3);
                }
            }
        }
    }
}
