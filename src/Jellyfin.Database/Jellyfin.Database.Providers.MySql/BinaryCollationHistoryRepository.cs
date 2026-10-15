using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microting.EntityFrameworkCore.MySql.Migrations.Internal;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Creates the migration history table with the collation of <see cref="MySqlDatabaseProvider.Collation"/>, like every
/// table of the model.
/// </summary>
/// <remarks>
/// EF Core creates the history table before it applies the first migration, so the collation of the database does not
/// reach it, and the provider of Microting names only the character set of the table and its columns. The server then
/// gives them the default collation of that character set, which is case-insensitive, whatever the database default is.
/// </remarks>
#pragma warning disable EF1001 // The history repository of the provider is internal, and deriving from it is the only way to configure the table.
internal sealed class BinaryCollationHistoryRepository : MySqlHistoryRepository
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
        history.UseCollation(MySqlDatabaseProvider.Collation, DelegationModes.ApplyToAll);
    }
}
#pragma warning restore EF1001
