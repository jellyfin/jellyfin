using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.ShareLinks;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.ShareLinks;

/// <summary>
/// Repository for scoped media share links backed by <see cref="JellyfinDbContext"/>.
/// </summary>
/// <param name="dbProvider">Efcore Factory.</param>
public class ShareLinkRepository(IDbContextFactory<JellyfinDbContext> dbProvider) : IShareLinkRepository
{
    /// <inheritdoc />
    public async Task<ShareLink?> GetByIdAsync(string jti, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.ShareLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == jti, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CreateAsync(ShareLink shareLink, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.ShareLinks.Add(shareLink);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TrySetFirstAccessAsync(string jti, long unixSecondsFirstAccessed, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var updated = await dbContext.ShareLinks
            .Where(e => e.Id == jti && e.FirstAccessedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(e => e.FirstAccessedAt, unixSecondsFirstAccessed),
                cancellationToken)
            .ConfigureAwait(false);
        return updated == 1;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAsync(string jti, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var updated = await dbContext.ShareLinks
            .Where(e => e.Id == jti && !e.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(e => e.IsRevoked, true),
                cancellationToken)
            .ConfigureAwait(false);
        return updated == 1;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShareLink>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.ShareLinks
            .AsNoTracking()
            .Where(e => e.UserId.Equals(userId))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShareLink>> ListByItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.ShareLinks
            .AsNoTracking()
            .Where(e => e.UserId.Equals(userId) && e.ItemId.Equals(itemId))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteUserLinksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.ShareLinks
            .Where(e => e.UserId.Equals(userId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> PurgeExpiredAsync(long unixNow, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.ShareLinks
            .Where(e => e.IsRevoked
                || (e.ExpiresAt != null && e.ExpiresAt < unixNow)
                || (e.DurationSeconds != null
                    && e.FirstAccessedAt != null
                    && e.FirstAccessedAt + e.DurationSeconds <= unixNow))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
