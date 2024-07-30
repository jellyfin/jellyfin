using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EFCoreSecondLevelCacheInterceptor;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.MediaSegments;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.MediaSegments;

/// <summary>
///     Manages media segments retrival and storage.
/// </summary>
public class MediaSegmentManager : IMediaSegmentManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IMediaSegmentProvider[] _segmentProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentManager"/> class.
    /// </summary>
    /// <param name="dbProvider">EFCore Database factory.</param>
    /// <param name="segmentProviders">List of all media segment providers.</param>
    public MediaSegmentManager(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IEnumerable<IMediaSegmentProvider> segmentProviders)
    {
        _dbProvider = dbProvider;

        _segmentProviders = segmentProviders
            .OrderBy(i => i is IHasOrder hasOrder ? hasOrder.Order : 0)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<MediaSegmentDto> CreateSegmentAsync(MediaSegmentDto mediaSegment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(mediaSegment.EndTicks, mediaSegment.StartTicks);

        using var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
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
    public async Task<IEnumerable<MediaSegmentDto>> GetSegmentsAsync(Guid itemId, IEnumerable<MediaSegmentType>? typeFilter)
    {
        using var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);

        var query = db.MediaSegments
            .Where(e => e.ItemId.Equals(itemId));

        if (typeFilter is not null)
        {
            query = query.Where(e => typeFilter.Contains(e.Type));
        }

        return query
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

    /// <inheritdoc/>
    public IEnumerable<(string Name, string Id)> GetSupportedProviders(BaseItem item)
    {
        if (item is not (Video or Audio))
        {
            return [];
        }

        return _segmentProviders
            .Select(p => (p.Name, GetProviderId(p.Name)))
            .ToImmutableArray();
    }

    private string GetProviderId(string name)
        => name.ToLowerInvariant()
            .GetMD5()
            .ToString("N", CultureInfo.InvariantCulture);
}
