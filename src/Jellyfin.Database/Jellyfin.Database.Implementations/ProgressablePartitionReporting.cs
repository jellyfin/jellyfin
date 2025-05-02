using System;
using System.Diagnostics;
using System.Linq;

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

    internal IOrderedQueryable<TEntity> Source => _source;

    internal void BeginItem(TEntity entity, int itteration, int itemIndex)
    {
        _itemTime.Restart();
        OnBeginItem?.Invoke(entity, itteration, itemIndex);
    }

    internal void BeginPartition(int itteration)
    {
        _partitionTime.Restart();
        OnBeginPartition?.Invoke(itteration);
    }

    internal void EndItem(TEntity entity, int itteration, int itemIndex)
    {
        OnEndItem?.Invoke(entity, itteration, itemIndex, _itemTime.Elapsed);
    }

    internal void EndPartition(int itteration)
    {
        OnEndPartition?.Invoke(itteration, _partitionTime.Elapsed);
    }
}
