using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Stores text that is not part of a key, a foreign key or an index as <c>nvarchar(max)</c>, whatever maximum length the
/// model gives it.
/// </summary>
/// <remarks>
/// The maximum length of a text column is only a statement about the model: the other providers store a longer value
/// whole. An <c>nvarchar(n)</c> column rejects a longer value, so such a column would turn a value that every other
/// provider accepts into a failed save. Text that is indexed keeps its length, as it is the value that the index and the
/// checks of the keys are sized for.
/// </remarks>
internal sealed class UnindexedTextConvention : IModelFinalizingConvention
{
    /// <summary>
    /// The type of the column of such text.
    /// </summary>
    internal const string ColumnType = "nvarchar(max)";

    /// <inheritdoc/>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (property.ClrType == typeof(string)
                    && property.GetMaxLength() is not null
                    && property.GetColumnTypeConfigurationSource() is null
                    && !property.IsKey()
                    && !property.IsForeignKey()
                    && !property.GetContainingIndexes().Any())
                {
                    property.Builder.HasColumnType(ColumnType);
                }
            }
        }
    }
}
