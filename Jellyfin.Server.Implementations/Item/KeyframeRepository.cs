using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Repository for obtaining Keyframe data.
/// </summary>
public class KeyframeRepository : IKeyframeRepository
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyframeRepository"/> class.
    /// </summary>
    /// <param name="dbProvider">The EFCore db factory.</param>
    public KeyframeRepository(IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _dbProvider = dbProvider;
    }

    private static MediaEncoding.Keyframes.KeyframeData Map(KeyframeData entity)
    {
        return new MediaEncoding.Keyframes.KeyframeData(
            entity.TotalDuration,
            (entity.KeyframeTicks ?? []).ToList());
    }

    private KeyframeData Map(MediaEncoding.Keyframes.KeyframeData dto, Guid itemId)
    {
        return new()
        {
            ItemId = itemId,
            TotalDuration = dto.TotalDuration,
            KeyframeTicks = dto.KeyframeTicks.ToList()
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<MediaEncoding.Keyframes.KeyframeData> GetKeyframeData(Guid itemId)
    {
        using var context = _dbProvider.CreateDbContext();

        return context.KeyframeData.AsNoTracking().Where(e => e.ItemId.Equals(itemId)).Select(e => Map(e)).ToList();
    }

    /// <inheritdoc />
    public async Task SaveKeyframeDataAsync(Guid itemId, MediaEncoding.Keyframes.KeyframeData data, CancellationToken cancellationToken)
    {
        using var context = _dbProvider.CreateDbContext();
        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await context.KeyframeData.Where(e => e.ItemId.Equals(itemId)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.KeyframeData.AddAsync(Map(data, itemId), cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteKeyframeDataAsync(Guid itemId, CancellationToken cancellationToken)
    {
        using var context = _dbProvider.CreateDbContext();
        await context.KeyframeData.Where(e => e.ItemId.Equals(itemId)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
