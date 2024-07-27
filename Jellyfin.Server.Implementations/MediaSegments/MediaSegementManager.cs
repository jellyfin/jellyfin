using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller;
using MediaBrowser.Model.MediaSegments;
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
    public async Task<MediaSegmentModel> CreateSegmentAsync(MediaSegmentModel mediaSegment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(mediaSegment.EndTick, mediaSegment.StartTick);

        using var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        mediaSegment.Id = Guid.NewGuid();
        db.MediaSegments.Add(Map(mediaSegment));
        await db.SaveChangesAsync().ConfigureAwait(false);
        return mediaSegment;
    }

    /// <inheritdoc />
    public async Task DeleteSegmentAsync(Guid segmentId)
    {
        using var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await db.MediaSegments.Where(e => e.Id.Equals(segmentId)).ExecuteDeleteAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MediaSegmentModel> GetSegmentsAsync(Guid itemId)
    {
        using var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var segment in db.MediaSegments
            .Where(e => e.ItemId.Equals(itemId))
            .OrderBy(e => e.StartTick)
            .AsAsyncEnumerable())
        {
            yield return Map(segment);
        }
    }

    private static MediaSegmentModel Map(MediaSegment segment)
    {
        return new MediaSegmentModel()
        {
            Id = segment.Id,
            EndTick = segment.EndTick,
            ItemId = segment.ItemId,
            StartTick = segment.StartTick,
            Type = (MediaSegmentTypeModel)segment.Type
        };
    }

    private static MediaSegment Map(MediaSegmentModel segment)
    {
        return new MediaSegment()
        {
            Id = segment.Id,
            EndTick = segment.EndTick,
            ItemId = segment.ItemId,
            StartTick = segment.StartTick,
            Type = (MediaSegmentType)segment.Type
        };
    }

    /// <inheritdoc />
    public bool HasSegments(Guid itemId)
    {
        using var db = _dbProvider.CreateDbContext();
        return db.MediaSegments.Any(e => e.ItemId.Equals(itemId));
    }
}
