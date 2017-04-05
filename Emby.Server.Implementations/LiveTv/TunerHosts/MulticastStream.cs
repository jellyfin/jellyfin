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
                    var allStreams = _outputStreams.ToList();

                    //if (allStreams.Count == 1)
                    //{
                    //    await allStreams[0].Value.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                    //}
                    //else
                    {
                        byte[] copy = new byte[bytesRead];
                        Buffer.BlockCopy(buffer, 0, copy, 0, bytesRead);

                        foreach (var stream in allStreams)
                        {
                            stream.Value.Queue(copy, 0, copy.Length);
                        }
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

        private static int RtpHeaderBytes = 12;
        public async Task CopyUntilCancelled(ISocket udpClient, Action onStarted, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            while (!cancellationToken.IsCancellationRequested)
            {
                var receiveToken = cancellationToken;

                // On the first connection attempt, put a timeout to avoid being stuck indefinitely in the event of failure
                if (onStarted != null)
                {
                    receiveToken = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(5000).Token, cancellationToken).Token;
                }

                var data = await udpClient.ReceiveAsync(receiveToken).ConfigureAwait(false);
                var bytesRead = data.ReceivedBytes - RtpHeaderBytes;

                if (bytesRead > 0)
                {
                    var allStreams = _outputStreams.ToList();

                    if (allStreams.Count == 1)
                    {
                        await allStreams[0].Value.WriteAsync(data.Buffer, 0, bytesRead).ConfigureAwait(false);
                    }
                    else
                    {
                        byte[] copy = new byte[bytesRead];
                        Buffer.BlockCopy(data.Buffer, RtpHeaderBytes, copy, 0, bytesRead);

                        foreach (var stream in allStreams)
                        {
                            stream.Value.Queue(copy, 0, copy.Length);
                        }
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
