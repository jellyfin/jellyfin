#pragma warning disable RS0030 // Do not use banned APIs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Recreates the placeholder BaseItem used to detach UserData on item deletion
/// (see migration DetachUserDataInsteadOfDelete) if it is missing.
/// Without it, every <see cref="BaseItemRepository.DeleteItem"/> call fails with a
/// FOREIGN KEY constraint violation, which library scans swallow per-item, leaving the
/// scan looking like it worked while silently corrupting delete/watch-state handling.
/// </summary>
[JellyfinMigration("2026-07-20T00:00:00", nameof(EnsureUserDataPlaceholderItem))]
internal class EnsureUserDataPlaceholderItem : IAsyncMigrationRoutine
{
    private readonly IStartupLogger _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;

    public EnsureUserDataPlaceholderItem(
        IStartupLogger<EnsureUserDataPlaceholderItem> startupLogger,
        IDbContextFactory<JellyfinDbContext> provider)
    {
        _logger = startupLogger;
        _provider = provider;
    }

    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var dbContext = await _provider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var exists = await dbContext.BaseItems
                .AnyAsync(e => e.Id == BaseItemRepository.PlaceholderId, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                return;
            }

            _logger.LogWarning(
                "UserData placeholder item {PlaceholderId} is missing. Recreating it now; " +
                "without it, item deletion during library scans fails silently with a FOREIGN KEY constraint error.",
                BaseItemRepository.PlaceholderId);

            dbContext.BaseItems.Add(new BaseItemEntity
            {
                Id = BaseItemRepository.PlaceholderId,
                Type = "PLACEHOLDER",
                Name = "This is a placeholder item for UserData that has been detacted from its original item"
            });

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
