#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixWatchEventBuffer : BackgroundService, ICustomNetflixWatchEventBuffer
{
    private const int Capacity = 16384;
    private const int MaxBatchSize = 250;
    private const int MaxTrackedProgressSessions = Capacity;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromSeconds(30);

    private readonly ICustomNetflixRepository _repository;
    private readonly CustomNetflixSchemaState _schemaState;
    private readonly ILogger<CustomNetflixWatchEventBuffer> _logger;
    private readonly ConcurrentDictionary<WatchEventSamplingKey, long> _progressSampleBuckets = new();
    private readonly Channel<BufferEntry> _channel = Channel.CreateBounded<BufferEntry>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    private int _pendingCount;

    public CustomNetflixWatchEventBuffer(
        ICustomNetflixRepository repository,
        CustomNetflixSchemaState schemaState,
        ILogger<CustomNetflixWatchEventBuffer> logger)
    {
        _repository = repository;
        _schemaState = schemaState;
        _logger = logger;
    }

    public async ValueTask EnqueueAsync(WatchEventRow watchEvent, CancellationToken cancellationToken)
    {
        if (!ShouldQueue(watchEvent))
        {
            CustomNetflixMetrics.ObserveWatchEventSample("skipped");
            return;
        }

        await _channel.Writer.WriteAsync(new BufferEntry(watchEvent, null), cancellationToken).ConfigureAwait(false);
        CustomNetflixMetrics.ObserveWatchEventSample("accepted");
        UpdateBufferMetrics();
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (!_repository.IsEnabled)
        {
            return;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _channel.Writer.WriteAsync(new BufferEntry(null, completion), cancellationToken).ConfigureAwait(false);
        await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pending = new Dictionary<WatchEventBufferKey, WatchEventRow>();
        var flushRequests = new List<TaskCompletionSource>();
        try
        {
            if (!_repository.IsEnabled)
            {
                return;
            }

            await _schemaState.WaitUntilReadyAsync(stoppingToken).ConfigureAwait(false);
            var failureCount = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                if (pending.Count == 0
                    && !await _channel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
                {
                    break;
                }

                if (failureCount == 0)
                {
                    DrainAvailable(pending, flushRequests);
                    if (pending.Count < MaxBatchSize && flushRequests.Count == 0)
                    {
                        await Task.Delay(FlushInterval, stoppingToken).ConfigureAwait(false);
                        DrainAvailable(pending, flushRequests);
                    }
                }

                if (await FlushAsync(pending, stoppingToken).ConfigureAwait(false))
                {
                    failureCount = 0;
                    CompleteFlushRequests(flushRequests);
                }
                else
                {
                    await Task.Delay(
                        CustomNetflixRetryPolicy.GetDelay(++failureCount, MaximumRetryDelay),
                        stoppingToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            DrainAvailable(pending, flushRequests);
            if (pending.Count > 0 && _schemaState.IsReady)
            {
                if (await FlushAsync(pending, CancellationToken.None).ConfigureAwait(false))
                {
                    CompleteFlushRequests(flushRequests);
                }
            }

            CancelFlushRequests(flushRequests, stoppingToken);
            while (_channel.Reader.TryRead(out var entry))
            {
                entry.FlushCompletion?.TrySetCanceled(stoppingToken);
            }
        }
    }

    private void DrainAvailable(
        Dictionary<WatchEventBufferKey, WatchEventRow> pending,
        List<TaskCompletionSource> flushRequests)
    {
        var drained = 0;
        while (drained < MaxBatchSize
               && pending.Count < MaxBatchSize
               && _channel.Reader.TryRead(out var entry))
        {
            drained++;
            if (entry.FlushCompletion is not null)
            {
                flushRequests.Add(entry.FlushCompletion);
                continue;
            }

            var watchEvent = entry.Value!;
            var key = CustomNetflixWatchEventBufferPolicy.GetKey(watchEvent);
            pending[key] = CustomNetflixWatchEventBufferPolicy.Coalesce(
                pending.GetValueOrDefault(key),
                watchEvent);
        }

        Volatile.Write(ref _pendingCount, pending.Count);
        UpdateBufferMetrics();
    }

    private bool ShouldQueue(WatchEventRow watchEvent)
    {
        var key = CustomNetflixWatchEventBufferPolicy.GetSamplingKey(watchEvent);
        if (!CustomNetflixWatchEventBufferPolicy.IsProgress(watchEvent))
        {
            _progressSampleBuckets.TryRemove(key, out _);
            return true;
        }

        // ponytail: bounded in-memory sampling state; use expiring entries only if abandoned sessions regularly hit this ceiling.
        if (_progressSampleBuckets.Count >= MaxTrackedProgressSessions
            && !_progressSampleBuckets.ContainsKey(key))
        {
            _progressSampleBuckets.Clear();
        }

        var bucket = CustomNetflixWatchEventBufferPolicy.GetProgressSampleBucket(watchEvent);
        while (true)
        {
            if (!_progressSampleBuckets.TryGetValue(key, out var currentBucket))
            {
                if (_progressSampleBuckets.TryAdd(key, bucket))
                {
                    return true;
                }

                continue;
            }

            if (bucket <= currentBucket)
            {
                return false;
            }

            if (_progressSampleBuckets.TryUpdate(key, bucket, currentBucket))
            {
                return true;
            }
        }
    }

    private async Task<bool> FlushAsync(Dictionary<WatchEventBufferKey, WatchEventRow> pending, CancellationToken cancellationToken)
    {
        if (pending.Count == 0 || !_repository.IsEnabled)
        {
            pending.Clear();
            return true;
        }

        var events = new List<WatchEventRow>(pending.Values);
        try
        {
            await _repository.InsertWatchEventsAsync(events, cancellationToken).ConfigureAwait(false);
            pending.Clear();
            Volatile.Write(ref _pendingCount, 0);
            CustomNetflixMetrics.ObserveBufferFlush("events", "success", events.Count);
            UpdateBufferMetrics();
            return true;
        }
        catch (Exception ex)
        {
            CustomNetflixMetrics.ObserveBufferFlush("events", "failure", events.Count);
            _logger.LogWarning(ex, "Failed to flush {Count} CustomNetflix watch events.", events.Count);
            return false;
        }
    }

    private void UpdateBufferMetrics()
        => CustomNetflixMetrics.SetBufferDepth("events", _channel.Reader.Count, Volatile.Read(ref _pendingCount));

    private static void CompleteFlushRequests(List<TaskCompletionSource> flushRequests)
    {
        foreach (var request in flushRequests)
        {
            request.TrySetResult();
        }

        flushRequests.Clear();
    }

    private static void CancelFlushRequests(
        List<TaskCompletionSource> flushRequests,
        CancellationToken cancellationToken)
    {
        foreach (var request in flushRequests)
        {
            request.TrySetCanceled(cancellationToken);
        }

        flushRequests.Clear();
    }

    private sealed record BufferEntry(WatchEventRow? Value, TaskCompletionSource? FlushCompletion);
}
