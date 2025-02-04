using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaSegments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.MediaSegments;

/// <summary>
/// Manages media segments retrieval and storage.
/// </summary>
public class MediaSegmentManager : IMediaSegmentManager
{
    private readonly ILogger<MediaSegmentManager> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IMediaSegmentProvider[] _segmentProviders;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentManager"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="dbProvider">EFCore Database factory.</param>
    /// <param name="segmentProviders">List of all media segment providers.</param>
    /// <param name="libraryManager">Library manager.</param>
    public MediaSegmentManager(
        ILogger<MediaSegmentManager> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IEnumerable<IMediaSegmentProvider> segmentProviders,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _dbProvider = dbProvider;

        _segmentProviders = segmentProviders
            .OrderBy(i => i is IHasOrder hasOrder ? hasOrder.Order : 0)
            .ToArray();
        _libraryManager = libraryManager;
    }

    /// <inheritdoc/>
    public async Task RunSegmentPluginProviders(BaseItem baseItem, bool overwrite, CancellationToken cancellationToken)
    {
        var libraryOptions = _libraryManager.GetLibraryOptions(baseItem);
        var providers = _segmentProviders
            .Where(e => !libraryOptions.DisabledMediaSegmentProviders.Contains(GetProviderId(e.Name)))
            .OrderBy(i =>
                {
                    var index = libraryOptions.MediaSegmentProviderOrder.IndexOf(i.Name);
                    return index == -1 ? int.MaxValue : index;
                })
            .ToList();

        if (providers.Count == 0)
        {
            _logger.LogDebug("Skipping media segment extraction as no providers are enabled for {MediaPath}", baseItem.Path);
            return;
        }

        using var db = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        if (!overwrite && (await db.MediaSegments.AnyAsync(e => e.ItemId.Equals(baseItem.Id), cancellationToken).ConfigureAwait(false)))
        {
            _logger.LogDebug("Skip {MediaPath} as it already contains media segments", baseItem.Path);
            return;
        }

        _logger.LogDebug("Start media segment extraction for {MediaPath} with {CountProviders} providers enabled", baseItem.Path, providers.Count);

        await db.MediaSegments.Where(e => e.ItemId.Equals(baseItem.Id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        // no need to recreate the request object every time.
        var requestItem = new MediaSegmentGenerationRequest() { ItemId = baseItem.Id };

        foreach (var provider in providers)
        {
            if (!await provider.Supports(baseItem).ConfigureAwait(false))
            {
                _logger.LogDebug("Media Segment provider {ProviderName} does not support item with path {MediaPath}", provider.Name, baseItem.Path);
                continue;
            }

            try
            {
                var segments = await provider.GetMediaSegments(requestItem, cancellationToken)
                    .ConfigureAwait(false);
                if (segments.Count == 0)
                {
                    _logger.LogDebug("Media Segment provider {ProviderName} did not find any segments for {MediaPath}", provider.Name, baseItem.Path);
                    continue;
                }

                _logger.LogInformation("Media Segment provider {ProviderName} found {CountSegments} for {MediaPath}", provider.Name, segments.Count, baseItem.Path);
                var providerId = GetProviderId(provider.Name);
                foreach (var segment in segments)
                {
                    segment.ItemId = baseItem.Id;
                    await CreateSegmentAsync(segment, providerId).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {ProviderName} failed to extract segments from {MediaPath}", provider.Name, baseItem.Path);
            }
        }
    }

    /// <inheritdoc />
    public async Task<MediaSegmentDto> CreateSegmentAsync(MediaSegmentDto mediaSegment, string segmentProviderId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(mediaSegment.EndTicks, mediaSegment.StartTicks);

        using var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        db.MediaSegments.Add(Map(mediaSegment, segmentProviderId));
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
    public async Task<IEnumerable<MediaSegmentDto>> GetSegmentsAsync(Guid itemId, IEnumerable<MediaSegmentType>? typeFilter, bool filterByProvider = true)
    {
        var baseItem = _libraryManager.GetItemById(itemId);

        if (baseItem is null)
        {
            _logger.LogError("Tried to request segments for an invalid item");
            return [];
        }

        return await GetSegmentsAsync(baseItem, typeFilter, filterByProvider).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MediaSegmentDto>> GetSegmentsAsync(BaseItem item, IEnumerable<MediaSegmentType>? typeFilter, bool filterByProvider = true)
    {
        using var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);

        var query = db.MediaSegments
            .Where(e => e.ItemId.Equals(item.Id));

        if (typeFilter is not null)
        {
            query = query.Where(e => typeFilter.Contains(e.Type));
        }

        if (filterByProvider)
        {
            var libraryOptions = _libraryManager.GetLibraryOptions(item);
            var providerIds = _segmentProviders
                .Where(e => !libraryOptions.DisabledMediaSegmentProviders.Contains(GetProviderId(e.Name)))
                .Select(f => GetProviderId(f.Name))
                .ToArray();
            if (providerIds.Length == 0)
            {
                return [];
            }

            query = query.Where(e => providerIds.Contains(e.SegmentProviderId));
        }

        return query
            .OrderBy(e => e.StartTicks)
            .AsNoTracking()
            .AsEnumerable()
            .Select(Map)
            .ToArray();
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

    private static MediaSegment Map(MediaSegmentDto segment, string segmentProviderId)
    {
        return new MediaSegment()
        {
            Id = segment.Id,
            EndTicks = segment.EndTicks,
            ItemId = segment.ItemId,
            StartTicks = segment.StartTicks,
            Type = segment.Type,
            SegmentProviderId = segmentProviderId
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
            .Select(p => (p.Name, GetProviderId(p.Name)));
    }

    private string GetProviderId(string name)
        => name.ToLowerInvariant()
            .GetMD5()
            .ToString("N", CultureInfo.InvariantCulture);
}
