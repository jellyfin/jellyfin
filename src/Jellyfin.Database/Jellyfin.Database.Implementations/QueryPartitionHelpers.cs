using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    // A row that cannot be read leaves the enumerator's position undefined. Skipping is only safe while it
    // keeps moving, so a run of failures with no row in between is treated as a dead reader rather than as
    // more bad rows.
    private const int MaxConsecutiveItemFailures = 10;

    /// <summary>
    /// Reads the key of a row that could not be loaded, so the log can name it. Reading the key alone
    /// avoids the columns that made the row unreadable, but it is still best effort.
    /// </summary>
    private static async Task<object?> ReadKeyAsync<TEntity>(ProgressablePartitionReporting<TEntity> progressablePartition, int rowIndex, CancellationToken cancellationToken)
    {
        if (progressablePartition.KeyLookup is null)
        {
            return null;
        }

        try
        {
            return await progressablePartition.KeyLookup(rowIndex, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return null;
        }
    }

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
    /// Allows the following calls of Partition to skip rows the database cannot turn into an entity, instead
    /// of aborting. Without this the enumeration keeps its existing behaviour and the failure propagates.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <typeparam name="TKey">The key to report the offending row by.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="keySelector">Selects the key to report, read on its own so the unreadable columns are not touched.</param>
    /// <param name="onItemFailed">Invoked with the failure, the row's key when it could be read, and its index in the query.</param>
    /// <returns>A queryable that can be used to partition.</returns>
    public static ProgressablePartitionReporting<TEntity> SkippingUnreadableItems<TEntity, TKey>(
        this IOrderedQueryable<TEntity> query,
        Expression<Func<TEntity, TKey>> keySelector,
        Action<Exception, object?, int> onItemFailed)
        => new ProgressablePartitionReporting<TEntity>(query).SkippingUnreadableItems(keySelector, onItemFailed);

    /// <summary>
    /// Allows the following calls of Partition to skip rows the database cannot turn into an entity, instead
    /// of aborting. Without this the enumeration keeps its existing behaviour and the failure propagates.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <typeparam name="TKey">The key to report the offending row by.</typeparam>
    /// <param name="progressable">The source query.</param>
    /// <param name="keySelector">Selects the key to report, read on its own so the unreadable columns are not touched.</param>
    /// <param name="onItemFailed">Invoked with the failure, the row's key when it could be read, and its index in the query.</param>
    /// <returns>A queryable that can be used to partition.</returns>
    public static ProgressablePartitionReporting<TEntity> SkippingUnreadableItems<TEntity, TKey>(
        this ProgressablePartitionReporting<TEntity> progressable,
        Expression<Func<TEntity, TKey>> keySelector,
        Action<Exception, object?, int> onItemFailed)
    {
        ArgumentNullException.ThrowIfNull(progressable);

        var source = progressable.Source;
        progressable.OnItemFailed = onItemFailed;
        progressable.KeyLookup = async (index, cancellationToken) =>
            await source.Skip(index).Take(1).Select(keySelector).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return progressable;
    }

    /// <summary>
    /// Enumerates the source query by loading the entities in partitions in a lazy manner reading each item from the database as its requested.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="partitionInfo">The source query.</param>
    /// <param name="partitionSize">The number of elements to load per partition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
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
    /// <param name="cancellationToken">The cancellation token.</param>
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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A enumerable representing the whole of the query.</returns>
    public static async IAsyncEnumerable<TEntity> PartitionAsync<TEntity>(
        this IOrderedQueryable<TEntity> query,
        int partitionSize,
        ProgressablePartitionReporting<TEntity>? progressablePartition = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var iterator = 0;
        int rowsRead;
        do
        {
            progressablePartition?.BeginPartition(iterator);
            var itemCounter = 0;
            rowsRead = 0;
            var consecutiveFailures = 0;

            var enumerator = query
                .Skip(partitionSize * iterator)
                .Take(partitionSize)
                .AsAsyncEnumerable()
                .GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                while (true)
                {
                    TEntity item;
                    try
                    {
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            break;
                        }

                        item = enumerator.Current;
                        consecutiveFailures = 0;
                    }
                    catch (Exception ex)
                    {
                        consecutiveFailures++;
                        if (progressablePartition?.OnItemFailed is null || consecutiveFailures > MaxConsecutiveItemFailures)
                        {
                            throw;
                        }

                        var rowIndex = (partitionSize * iterator) + rowsRead;
                        progressablePartition.ItemFailed(ex, await ReadKeyAsync(progressablePartition, rowIndex, cancellationToken).ConfigureAwait(false), rowIndex);
                        rowsRead++;
                        continue;
                    }

                    rowsRead++;
                    progressablePartition?.BeginItem(item, iterator, itemCounter);
                    yield return item;
                    progressablePartition?.EndItem(item, iterator, itemCounter);
                    itemCounter++;
                }
            }

            progressablePartition?.EndPartition(iterator);
            iterator++;
            // Counting rows rather than yielded items, so a skipped row cannot end the walk early.
        } while (rowsRead == partitionSize && !cancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// Enumerates the source query by loading the entities in partitions directly into memory.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="partitionSize">The number of elements to load per partition.</param>
    /// <param name="progressablePartition">Reporting helper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A enumerable representing the whole of the query.</returns>
    public static async IAsyncEnumerable<TEntity> PartitionEagerAsync<TEntity>(
        this IOrderedQueryable<TEntity> query,
        int partitionSize,
        ProgressablePartitionReporting<TEntity>? progressablePartition = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var iterator = 0;
        int rowsRead;
        var items = ArrayPool<TEntity>.Shared.Rent(partitionSize);
        try
        {
            do
            {
                progressablePartition?.BeginPartition(iterator);
                var itemCounter = 0;
                rowsRead = 0;
                var consecutiveFailures = 0;

                var enumerator = query
                    .Skip(partitionSize * iterator)
                    .Take(partitionSize)
                    .AsAsyncEnumerable()
                    .GetAsyncEnumerator(cancellationToken);
                await using (enumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        try
                        {
                            if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                            {
                                break;
                            }

                            items[itemCounter++] = enumerator.Current;
                            consecutiveFailures = 0;
                        }
                        catch (Exception ex)
                        {
                            consecutiveFailures++;
                            if (progressablePartition?.OnItemFailed is null || consecutiveFailures > MaxConsecutiveItemFailures)
                            {
                                throw;
                            }

                            var rowIndex = (partitionSize * iterator) + rowsRead;
                            progressablePartition.ItemFailed(ex, await ReadKeyAsync(progressablePartition, rowIndex, cancellationToken).ConfigureAwait(false), rowIndex);
                            rowsRead++;
                            continue;
                        }

                        rowsRead++;
                    }
                }

                for (int i = 0; i < itemCounter; i++)
                {
                    progressablePartition?.BeginItem(items[i], iterator, itemCounter);
                    yield return items[i];
                    progressablePartition?.EndItem(items[i], iterator, itemCounter);
                }

                progressablePartition?.EndPartition(iterator);
                iterator++;
                // Counting rows rather than yielded items, so a skipped row cannot end the walk early.
            } while (rowsRead == partitionSize && !cancellationToken.IsCancellationRequested);
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
