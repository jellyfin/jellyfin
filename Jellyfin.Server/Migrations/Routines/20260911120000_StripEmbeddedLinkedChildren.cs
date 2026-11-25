using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Drops keys that no current property reads or writes from the serialized <c>BaseItems.Data</c> blob.
/// </summary>
[JellyfinMigration("2026-09-11T12:00:00", nameof(StripEmbeddedLinkedChildren))]
internal class StripEmbeddedLinkedChildren : IDatabaseMigrationRoutine
{
    private readonly ILogger<StripEmbeddedLinkedChildren> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    public StripEmbeddedLinkedChildren(
        ILoggerFactory loggerFactory,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = loggerFactory.CreateLogger<StripEmbeddedLinkedChildren>();
        _dbProvider = dbProvider;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // json_valid guards the rare malformed blob: json_remove would abort the statement on it,
            // and one bad row must not cost every other row the fix.
            var updated = await context.Database.ExecuteSqlRawAsync(
                """
                UPDATE "BaseItems"
                SET "Data" = json_remove("Data", '$.LinkedChildren', '$.ExtraIds', '$.SupportsExternalTransfer')
                WHERE "Data" IS NOT NULL
                  AND json_valid("Data") = 1
                  AND ("Data" LIKE '%"LinkedChildren"%'
                    OR "Data" LIKE '%"ExtraIds"%'
                    OR "Data" LIKE '%"SupportsExternalTransfer"%')
                """,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Dropped dead keys from the serialized data of {Count} items", updated);
        }
    }
}
