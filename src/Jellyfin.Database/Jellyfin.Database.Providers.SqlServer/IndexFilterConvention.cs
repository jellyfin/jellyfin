using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Removes the filter of an index whose filter has an <c>OR</c>, which SQL Server cannot create. The index stays, over
/// every row, and every other filter stays.
/// </summary>
/// <remarks>
/// A filtered index on SQL Server takes only a conjunction of simple comparisons. A filter that was set explicitly can
/// only be removed through the mutable model, as a convention cannot override it. Without the filter the index is larger
/// and is used by more queries, and it accepts the same rows, as long as it is not unique: the convention refuses to
/// remove the filter of a unique index, which would then refuse rows the filter left out.
/// </remarks>
internal sealed partial class IndexFilterConvention : IModelFinalizingConvention
{
    /// <summary>
    /// Gets whether SQL Server can create an index with a filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>Whether the filter has no <c>OR</c>.</returns>
    internal static bool CanExpress(string filter) => !OrKeyword().IsMatch(filter);

    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var index in modelBuilder.Metadata.GetEntityTypes().SelectMany(e => e.GetDeclaredIndexes()))
        {
            if (index.GetFilter() is { } filter && !CanExpress(filter))
            {
                if (index.IsUnique)
                {
                    throw new System.InvalidOperationException($"The unique index {index.GetDatabaseName()} has the filter {filter}, which SQL Server cannot create.");
                }

                ((IMutableIndex)index).SetFilter(null);
            }
        }
    }

    [GeneratedRegex(@"\bOR\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrKeyword();
}
