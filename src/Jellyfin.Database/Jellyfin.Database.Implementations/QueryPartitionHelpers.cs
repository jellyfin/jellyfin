using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// Enumerates the source query by loading the entities in partitions in a lazy manner reading each item from the database as its requested.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="partitionSize">The number of elements to load per partition.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A enumerable representing the whole of the query.</returns>
    public static async IAsyncEnumerable<TEntity> PartitionAsync<TEntity>(this IOrderedQueryable<TEntity> query, int partitionSize, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var itterator = 0;
        int itemCounter;
        do
        {
            itemCounter = 0;
            await foreach (var item in query
                .Skip(partitionSize * itterator)
                .Take(partitionSize)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return item;
                itemCounter++;
            }

            itterator++;
        } while (itemCounter == partitionSize && !cancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// Enumerates the source query by loading the entities in partitions directly into memory.
    /// </summary>
    /// <typeparam name="TEntity">The entity to load.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="partitionSize">The number of elements to load per partition.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A enumerable representing the whole of the query.</returns>
    public static async IAsyncEnumerable<TEntity> PartitionEagerAsync<TEntity>(this IOrderedQueryable<TEntity> query, int partitionSize, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var itterator = 0;
        int itemCounter;
        var items = ArrayPool<TEntity>.Shared.Rent(partitionSize);
        try
        {
            do
            {
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
                    yield return items[i];
                }

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
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>The source list with an index added.</returns>
    public static async IAsyncEnumerable<(TEntity Item, int Index)> WithIndex<TEntity>(this IAsyncEnumerable<TEntity> query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var index = 0;
        await foreach (var item in query.ConfigureAwait(false))
        {
            yield return (item, index++);
        }
    }
}
