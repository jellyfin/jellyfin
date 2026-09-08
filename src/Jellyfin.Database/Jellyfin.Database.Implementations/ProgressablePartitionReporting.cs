using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Wrapper for progress reporting on Partition helpers.
/// </summary>
/// <typeparam name="TEntity">The entity to load.</typeparam>
public class ProgressablePartitionReporting<TEntity>
{
    private readonly IOrderedQueryable<TEntity> _source;

    private readonly Stopwatch _partitionTime = new();

    private readonly Stopwatch _itemTime = new();

    internal ProgressablePartitionReporting(IOrderedQueryable<TEntity> source)
    {
        _source = source;
    }

    internal Action<TEntity, int, int>? OnBeginItem { get; set; }

    internal Action<int>? OnBeginPartition { get; set; }

    internal Action<TEntity, int, int, TimeSpan>? OnEndItem { get; set; }

    internal Action<int, TimeSpan>? OnEndPartition { get; set; }

    internal Action<Exception, object?, int>? OnItemFailed { get; set; }

    internal Func<int, CancellationToken, Task<object?>>? KeyLookup { get; set; }

    internal IOrderedQueryable<TEntity> Source => _source;

    internal void BeginItem(TEntity entity, int iteration, int itemIndex)
    {
        _itemTime.Restart();
        OnBeginItem?.Invoke(entity, iteration, itemIndex);
    }

    internal void BeginPartition(int iteration)
    {
        _partitionTime.Restart();
        OnBeginPartition?.Invoke(iteration);
    }

    internal void EndItem(TEntity entity, int iteration, int itemIndex)
    {
        OnEndItem?.Invoke(entity, iteration, itemIndex, _itemTime.Elapsed);
    }

    internal void EndPartition(int iteration)
    {
        OnEndPartition?.Invoke(iteration, _partitionTime.Elapsed);
    }

    internal void ItemFailed(Exception exception, object? key, int rowIndex)
    {
        OnItemFailed!.Invoke(exception, key, rowIndex);
    }
}
