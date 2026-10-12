using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Jellyfin.Database.Providers.Postgres;

/// <summary>
/// Gives every column of an index that can hold nulls the null order that matches its direction, nulls first for an
/// ascending column and nulls last for a descending one, as SQLite does.
/// </summary>
/// <remarks>
/// PostgreSQL puts nulls last in an ascending index. A query that orders ascending by a nullable column asks for nulls
/// first, so without this it could not be answered from the index. A descending column already has its nulls first in a
/// default PostgreSQL index, so it is given nulls last to match what a descending query asks for. A column that cannot
/// hold nulls keeps the default order, because the queries do not name a null placement for it and the index behind a
/// key, which cannot name one either, has the default order as well. Npgsql only reads the sort order from an array,
/// which is why the order is not stored as a list.
/// </remarks>
internal sealed class NullsFirstIndexConvention : IModelFinalizingConvention
{
    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var index in modelBuilder.Metadata.GetEntityTypes().SelectMany(e => e.GetDeclaredIndexes()))
        {
            // EF Core stores no list for an ascending index and an empty list when every column is descending.
            var descending = index.IsDescending;
            var order = index.Properties
                .Select((property, i) => property.IsNullable
                    ? (IsDescending(descending, i) ? NullSortOrder.NullsLast : NullSortOrder.NullsFirst)
                    : NullSortOrder.Unspecified)
                .ToArray();
            if (order.Any(o => o != NullSortOrder.Unspecified))
            {
                index.SetNullSortOrder(order, fromDataAnnotation: false);
            }
        }
    }

    private static bool IsDescending(IReadOnlyList<bool>? descending, int column)
    {
        return descending is not null && (descending.Count == 0 || descending[column]);
    }
}
