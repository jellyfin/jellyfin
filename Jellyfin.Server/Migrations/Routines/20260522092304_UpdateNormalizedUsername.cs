using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Part 2 Migration for NormalisedUsername.
/// </summary>
[JellyfinMigration("2026-05-22T09:23:04", nameof(UpdateNormalizedUsername), Stage = Stages.JellyfinMigrationStageTypes.CoreInitialisation)]
#pragma warning disable SA1649 // File name should match first type name
public class UpdateNormalizedUsername : IAsyncMigrationRoutine
#pragma warning restore SA1649 // File name should match first type name
{
    private readonly IDbContextFactory<JellyfinDbContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateNormalizedUsername"/> class.
    /// </summary>
    /// <param name="contextFactory">Db Context factory.</param>
    public UpdateNormalizedUsername(IDbContextFactory<JellyfinDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var users = await dbContext.Users.ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var user in users)
            {
                user.NormalizedUsername = user.Username.ToUpperInvariant();
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
