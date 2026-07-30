using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Creates a default Watchlist for users that do not already have one.
/// </summary>
[JellyfinMigration("2026-08-01T12:00:00", nameof(CreateDefaultUserLists))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class CreateDefaultUserLists : IAsyncMigrationRoutine
{
    private const int ProgressLogStep = 500;

    private readonly IStartupLogger<CreateDefaultUserLists> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateDefaultUserLists"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public CreateDefaultUserLists(
        IStartupLogger<CreateDefaultUserLists> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var users = await dbContext.Users
                .AsNoTracking()
                .Select(user => new
                {
                    user.Id,
                    HasDefaultList = user.Lists.Any(list => list.IsDefault),
                    MaximumSortIndex = user.Lists.Select(list => (int?)list.SortIndex).Max(),
                    ListCount = user.Lists.Count
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var existingDefaultListUserIds = users
                .Where(user => user.HasDefaultList)
                .Select(user => user.Id)
                .ToHashSet();
            var defaultLists = new List<UserList>(users.Count - existingDefaultListUserIds.Count);
            var now = DateTime.UtcNow;
            var processed = 0;
            var skipped = 0;

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                processed++;
                if (existingDefaultListUserIds.Contains(user.Id))
                {
                    skipped++;
                }
                else
                {
                    var sortIndex = user.MaximumSortIndex.HasValue && user.MaximumSortIndex.Value < int.MaxValue
                        ? user.MaximumSortIndex.Value + 1
                        : user.ListCount;
                    defaultLists.Add(new UserList
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Name = "Watchlist",
                        Kind = UserListKind.Watchlist,
                        IsDefault = true,
                        AutoRemoveWatched = true,
                        SortIndex = sortIndex,
                        DateCreated = now,
                        DateModified = now
                    });
                }

                if (processed % ProgressLogStep == 0)
                {
                    _logger.LogInformation(
                        "Provisioning default user lists: processed {Processed}/{Total}, staged {Provisioned}, skipped {Skipped}",
                        processed,
                        users.Count,
                        defaultLists.Count,
                        skipped);
                }
            }

            if (defaultLists.Count > 0)
            {
                dbContext.UserLists.AddRange(defaultLists);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Default user list provisioning complete: provisioned {Provisioned}, skipped {Skipped}",
                defaultLists.Count,
                skipped);
        }
    }
}
