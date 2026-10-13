using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Removes the filter of every index, because neither MySQL nor MariaDB can create a partial index.
/// </summary>
/// <remarks>
/// A filter that was set explicitly can only be removed through the mutable model, as a convention cannot override
/// it. The index stays, over all the rows. That is also what a filtered unique index becomes: rows that held a null
/// in the indexed column stay unique among themselves, because both servers allow more than one null in a unique index.
/// </remarks>
internal sealed class IndexFilterConvention : IModelFinalizingConvention
{
    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var index in modelBuilder.Metadata.GetEntityTypes().SelectMany(e => e.GetDeclaredIndexes()))
        {
            if (index.GetFilter() is not null)
            {
                ((IMutableIndex)index).SetFilter(null);
            }
        }
    }
}
