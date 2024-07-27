using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.MediaSegments;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations;

/// <summary>
///     Manages media segments retrival and storage.
/// </summary>
public class MediaSegmentManager : IMediaSegmentManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentManager"/> class.
    /// </summary>
    /// <param name="dbProvider">EFCore Database factory.</param>
    public MediaSegmentManager(IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task<MediaSegmentDto> CreateSegmentAsync(MediaSegmentDto mediaSegment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(mediaSegment.EndTicks, mediaSegment.StartTicks);

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
    public async Task<IEnumerable<MediaSegmentDto>> GetSegmentsAsync(Guid itemId, IEnumerable<MediaSegmentType> typeFilter)
    {
        using var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);

        return db.MediaSegments
            .Where(e => e.ItemId.Equals(itemId))
            .Where(e => typeFilter.Contains(e.Type))
            .OrderBy(e => e.StartTicks)
            .ToImmutableList()
            .Select(Map);
    }

    private static MediaSegmentDto Map(MediaSegment segment)
    {
        return new MediaSegmentDto()
        {
            Id = segment.Id,
            EndTicks = segment.EndTicks,
            ItemId = segment.ItemId,
            StartTicks = segment.StartTicks,
            Type = segment.Type
        };
    }

    private static MediaSegment Map(MediaSegmentDto segment)
    {
        return new MediaSegment()
        {
            Id = segment.Id,
            EndTicks = segment.EndTicks,
            ItemId = segment.ItemId,
            StartTicks = segment.StartTicks,
            Type = segment.Type
        };
    }

    /// <inheritdoc />
    public bool HasSegments(Guid itemId)
    {
        using var db = _dbProvider.CreateDbContext();
        return db.MediaSegments.Any(e => e.ItemId.Equals(itemId));
    }

    /// <inheritdoc/>
    public bool IsTypeSupported(BaseItem baseItem)
    {
        return baseItem.MediaType is Data.Enums.MediaType.Video or Data.Enums.MediaType.Audio;
    }
}
