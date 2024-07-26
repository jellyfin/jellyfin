using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations;

/// <summary>
///     Manages media segments retrival and storage.
/// </summary>
public class MediaSegementManager : IMediaSegmentManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegementManager"/> class.
    /// </summary>
    /// <param name="dbProvider">EFCore Database factory.</param>
    public MediaSegementManager(IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task<MediaSegment> CreateSegmentAsync(MediaSegment mediaSegment)
    {
        if (mediaSegment.EndTick < mediaSegment.StartTick)
        {
            throw new InvalidOperationException($"A segments {nameof(MediaSegment.EndTick)} cannot be before its {nameof(MediaSegment.StartTick)}");
        }

        using var db = await _dbProvider.CreateDbContextAsync();
        mediaSegment.Id = Guid.NewGuid();
        db.MediaSegments.Add(mediaSegment);
        await db.SaveChangesAsync();
        return mediaSegment;
    }

    /// <inheritdoc />
    public async Task DeleteSegmentAsync(Guid segmentId)
    {
        using var db = await _dbProvider.CreateDbContextAsync();
        await db.MediaSegments.Where(e => e.Id.Equals(segmentId)).ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MediaSegment> GetSegmentsAsync(Guid itemId)
    {
        using var db = await _dbProvider.CreateDbContextAsync();
        await foreach (var segment in db.MediaSegments.Where(e => e.ItemId.Equals(itemId)).AsAsyncEnumerable())
        {
            yield return segment;
        }
    }
}
