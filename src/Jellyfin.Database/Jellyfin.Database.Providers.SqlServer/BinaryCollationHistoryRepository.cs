using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Creates the migration history table with the collation of <see cref="SqlServerDatabaseProvider.Collation"/>, like every
/// text column of the model.
/// </summary>
/// <remarks>
/// EF Core creates the history table before it applies the first migration, and names no collation for its columns, so
/// they would take the default collation of the database, which is case-insensitive unless the operator chose another.
/// </remarks>
#pragma warning disable EF1001 // The history repository of the provider is internal, and deriving from it is the only way to configure the table.
internal sealed class BinaryCollationHistoryRepository : SqlServerHistoryRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryCollationHistoryRepository"/> class.
    /// </summary>
    /// <param name="dependencies">The dependencies of the repository.</param>
    public BinaryCollationHistoryRepository(HistoryRepositoryDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureTable(EntityTypeBuilder<HistoryRow> history)
    {
        base.ConfigureTable(history);
        history.Property(h => h.MigrationId).UseCollation(SqlServerDatabaseProvider.Collation);
        history.Property(h => h.ProductVersion).UseCollation(SqlServerDatabaseProvider.Collation);
    }
}
#pragma warning restore EF1001
