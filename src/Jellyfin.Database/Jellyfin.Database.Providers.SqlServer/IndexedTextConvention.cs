using Jellyfin.Database.Implementations.ModelConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Stores the strings that are indexed without a maximum length of their own as <c>nvarchar(4000)</c>, the longest type
/// that SQL Server can index.
/// </summary>
/// <remarks>
/// <c>nvarchar(max)</c> cannot be a column of an index at all. An index entry holds at most 1700 bytes of key, which is
/// 850 characters of such a column alone, so the server warns when it creates such an index, and refuses a row whose key
/// is longer.
/// </remarks>
internal sealed class IndexedTextConvention : IModelFinalizingConvention
{
    /// <summary>
    /// The type of the column of such a string.
    /// </summary>
    internal const string ColumnType = "nvarchar(4000)";

    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (property.IsUnboundedIndexedText())
                {
                    property.Builder.HasColumnType(ColumnType);
                }
            }
        }
    }
}
