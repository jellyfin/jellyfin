#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixWatchProgressBuffer : BackgroundService, ICustomNetflixWatchProgressBuffer
{
    private const int Capacity = 16384;
    private const int MaxBatchSize = 250;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromSeconds(30);

    private readonly ICustomNetflixRepository _repository;
    private readonly CustomNetflixSchemaState _schemaState;
    private readonly ILogger<CustomNetflixWatchProgressBuffer> _logger;
    private readonly Channel<BufferEntry> _channel = Channel.CreateBounded<BufferEntry>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    private int _pendingCount;

    public CustomNetflixWatchProgressBuffer(
        ICustomNetflixRepository repository,
        CustomNetflixSchemaState schemaState,
        ILogger<CustomNetflixWatchProgressBuffer> logger)
    {
        _repository = repository;
        _schemaState = schemaState;
        _logger = logger;
    }

    public async ValueTask EnqueueAsync(WatchProgressRow progress, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(new BufferEntry(progress, null), cancellationToken).ConfigureAwait(false);
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
        var pending = new Dictionary<WatchProgressBufferKey, WatchProgressRow>();
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
        Dictionary<WatchProgressBufferKey, WatchProgressRow> pending,
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

            var progress = entry.Value!;
            var key = CustomNetflixWatchProgressBufferPolicy.GetKey(progress);
            pending[key] = CustomNetflixWatchProgressBufferPolicy.Coalesce(
                pending.GetValueOrDefault(key),
                progress);
        }

        Volatile.Write(ref _pendingCount, pending.Count);
        UpdateBufferMetrics();
    }

    private async Task<bool> FlushAsync(Dictionary<WatchProgressBufferKey, WatchProgressRow> pending, CancellationToken cancellationToken)
    {
        if (pending.Count == 0 || !_repository.IsEnabled)
        {
            pending.Clear();
            return true;
        }

        var progressRows = new List<WatchProgressRow>(pending.Values);
        try
        {
            await _repository.UpsertProgressRowsAsync(progressRows, cancellationToken).ConfigureAwait(false);
            pending.Clear();
            Volatile.Write(ref _pendingCount, 0);
            CustomNetflixMetrics.ObserveBufferFlush("progress", "success", progressRows.Count);
            UpdateBufferMetrics();
            return true;
        }
        catch (Exception ex)
        {
            CustomNetflixMetrics.ObserveBufferFlush("progress", "failure", progressRows.Count);
            _logger.LogWarning(ex, "Failed to flush {Count} CustomNetflix watch progress rows.", progressRows.Count);
            return false;
        }
    }

    private void UpdateBufferMetrics()
        => CustomNetflixMetrics.SetBufferDepth("progress", _channel.Reader.Count, Volatile.Read(ref _pendingCount));

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

    private sealed record BufferEntry(WatchProgressRow? Value, TaskCompletionSource? FlushCompletion);
}
