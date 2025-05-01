using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Contains helpers to partition EFCore queries.
/// </summary>
public static class QueryPartitionHelpers
{
    /// <summary>
    /// Adds a callback to any directly following calls of Partition for every partition thats been invoked.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="beginPartition">The callback invoked for partition before enumerating items.</param>
    /// <param name="endPartition">The callback invoked for partition after enumerating items.</param>
    /// <returns>A queryable that can be used to partition.</returns>
    public static ProgressablePartitionReporting<TEntity> WithPartitionProgress<TEntity>(this IOrderedQueryable<TEntity> query, Action<int>? beginPartition = null, Action<int, TimeSpan>? endPartition = null)
    {
        var progressable = new ProgressablePartitionReporting<TEntity>(query);
        progressable.OnBeginPartition = beginPartition;
        progressable.OnEndPartition = endPartition;
        return progressable;
    }

    /// <summary>
    /// Adds a callback to any directly following calls of Partition for every item thats been invoked.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="beginItem">The callback invoked for each item before processing.</param>
    /// <param name="endItem">The callback invoked for each item after processing.</param>
    /// <returns>A queryable that can be used to partition.</returns>
    public static ProgressablePartitionReporting<TEntity> WithItemProgress<TEntity>(this IOrderedQueryable<TEntity> query, Action<TEntity, int, int>? beginItem = null, Action<TEntity, int, int, TimeSpan>? endItem = null)
    {
        var progressable = new ProgressablePartitionReporting<TEntity>(query);
        progressable.OnBeginItem = beginItem;
        progressable.OnEndItem = endItem;
        return progressable;
    }

    /// <summary>
    /// Adds a callback to any directly following calls of Partition for every partition thats been invoked.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="progressable">The source query.</param>
    /// <param name="beginPartition">The callback invoked for partition before enumerating items.</param>
    /// <param name="endPartition">The callback invoked for partition after enumerating items.</param>
    /// <returns>A queryable that can be used to partition.</returns>
    public static ProgressablePartitionReporting<TEntity> WithPartitionProgress<TEntity>(this ProgressablePartitionReporting<TEntity> progressable, Action<int>? beginPartition = null, Action<int, TimeSpan>? endPartition = null)
    {
        progressable.OnBeginPartition = beginPartition;
        progressable.OnEndPartition = endPartition;
        return progressable;
    }

    /// <summary>
    /// Adds a callback to any directly following calls of Partition for every item thats been invoked.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="progressable">The source query.</param>
    /// <param name="beginItem">The callback invoked for each item before processing.</param>
    /// <param name="endItem">The callback invoked for each item after processing.</param>
    /// <returns>A queryable that can be used to partition.</returns>
    public static ProgressablePartitionReporting<TEntity> WithItemProgress<TEntity>(this ProgressablePartitionReporting<TEntity> progressable, Action<TEntity, int, int>? beginItem = null, Action<TEntity, int, int, TimeSpan>? endItem = null)
    {
        progressable.OnBeginItem = beginItem;
        progressable.OnEndItem = endItem;
        return progressable;
    }

    /// <summary>
    /// Enumerates the source query by loading the entities in partitions in a lazy manner reading each item from the database as its requested.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="partitionInfo">The source query.</param>
    /// <param name="partitionSize">The number of elements to load per partition.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A enumerable representing the whole of the query.</returns>
    public static async IAsyncEnumerable<TEntity> PartitionAsync<TEntity>(this ProgressablePartitionReporting<TEntity> partitionInfo, int partitionSize, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in partitionInfo.Source.PartitionAsync(partitionSize, partitionInfo, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Enumerates the source query by loading the entities in partitions directly into memory.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="partitionInfo">The source query.</param>
    /// <param name="partitionSize">The number of elements to load per partition.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A enumerable representing the whole of the query.</returns>
    public static async IAsyncEnumerable<TEntity> PartitionEagerAsync<TEntity>(this ProgressablePartitionReporting<TEntity> partitionInfo, int partitionSize, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in partitionInfo.Source.PartitionEagerAsync(partitionSize, partitionInfo, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Enumerates the source query by loading the entities in partitions in a lazy manner reading each item from the database as its requested.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="partitionSize">The number of elements to load per partition.</param>
    /// <param name="progressablePartition">Reporting helper.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A enumerable representing the whole of the query.</returns>
    public static async IAsyncEnumerable<TEntity> PartitionAsync<TEntity>(
        this IOrderedQueryable<TEntity> query,
        int partitionSize,
        ProgressablePartitionReporting<TEntity>? progressablePartition = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var itterator = 0;
        int itemCounter;
        do
        {
            progressablePartition?.BeginPartition(itterator);
            itemCounter = 0;
            await foreach (var item in query
                .Skip(partitionSize * itterator)
                .Take(partitionSize)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                progressablePartition?.BeginItem(item, itterator, itemCounter);
                yield return item;
                progressablePartition?.EndItem(item, itterator, itemCounter);
                itemCounter++;
            }

            progressablePartition?.EndPartition(itterator);
            itterator++;
        } while (itemCounter == partitionSize && !cancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// Enumerates the source query by loading the entities in partitions directly into memory.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="partitionSize">The number of elements to load per partition.</param>
    /// <param name="progressablePartition">Reporting helper.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A enumerable representing the whole of the query.</returns>
    public static async IAsyncEnumerable<TEntity> PartitionEagerAsync<TEntity>(
        this IOrderedQueryable<TEntity> query,
        int partitionSize,
        ProgressablePartitionReporting<TEntity>? progressablePartition = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var itterator = 0;
        int itemCounter;
        var items = ArrayPool<TEntity>.Shared.Rent(partitionSize);
        try
        {
            do
            {
                progressablePartition?.BeginPartition(itterator);
                itemCounter = 0;
                await foreach (var item in query
                    .Skip(partitionSize * itterator)
                    .Take(partitionSize)
                    .AsAsyncEnumerable()
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    items[itemCounter++] = item;
                }

                for (int i = 0; i < itemCounter; i++)
                {
                    progressablePartition?.BeginItem(items[i], itterator, itemCounter);
                    yield return items[i];
                    progressablePartition?.EndItem(items[i], itterator, itemCounter);
                }

                progressablePartition?.EndPartition(itterator);
                itterator++;
            } while (itemCounter == partitionSize && !cancellationToken.IsCancellationRequested);
        }
        finally
        {
            ArrayPool<TEntity>.Shared.Return(items);
        }
    }

    /// <summary>
    /// Adds an Index to the enumeration of the async enumerable.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="query">The source query.</param>
    /// <returns>The source list with an index added.</returns>
    public static async IAsyncEnumerable<(TEntity Item, int Index)> WithIndex<TEntity>(this IAsyncEnumerable<TEntity> query)
    {
        var index = 0;
        await foreach (var item in query.ConfigureAwait(false))
        {
            yield return (item, index++);
        }
    }
}

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
