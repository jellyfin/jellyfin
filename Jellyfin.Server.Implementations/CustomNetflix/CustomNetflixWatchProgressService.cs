#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixWatchProgressService : ICustomNetflixWatchProgressService
{
    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixRepository _repository;
    private readonly ICustomNetflixCacheService _cache;
    private readonly ICustomNetflixWatchProgressBuffer _watchProgressBuffer;
    private readonly ICustomNetflixWatchEventBuffer _watchEventBuffer;
    private readonly ICustomNetflixNativePlaystateSyncService _nativePlaystateSyncService;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly CustomNetflixCardDtoCache _cardDtoCache;

    public CustomNetflixWatchProgressService(
        ICustomNetflixProfileService profileService,
        ICustomNetflixRepository repository,
        ICustomNetflixCacheService cache,
        ICustomNetflixWatchProgressBuffer watchProgressBuffer,
        ICustomNetflixWatchEventBuffer watchEventBuffer,
        ICustomNetflixNativePlaystateSyncService nativePlaystateSyncService,
        IUserManager userManager,
        ILibraryManager libraryManager,
        CustomNetflixCardDtoCache cardDtoCache)
    {
        _profileService = profileService;
        _repository = repository;
        _cache = cache;
        _watchProgressBuffer = watchProgressBuffer;
        _watchEventBuffer = watchEventBuffer;
        _nativePlaystateSyncService = nativePlaystateSyncService;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _cardDtoCache = cardDtoCache;
    }

    public async Task<CustomNetflixWatchProgressDto?> ReportProgressAsync(Guid jellyfinUserId, Guid profileId, CustomNetflixWatchProgressReportRequest request, CancellationToken cancellationToken)
    {
        var eventType = "unknown";
        try
        {
            var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
            var user = _userManager.GetUserById(jellyfinUserId);
            if (profile is null || user is null)
            {
                CustomNetflixMetrics.ObserveProgressReport("not_found", eventType);
                return null;
            }

            var item = _libraryManager.GetItemById<BaseItem>(request.ItemId, user);
            if (item is null)
            {
                CustomNetflixMetrics.ObserveProgressReport("not_found", eventType);
                return null;
            }

            var duration = Math.Max(0, request.DurationSeconds);
            var position = Math.Clamp(request.PositionSeconds, 0, duration == 0 ? double.MaxValue : duration);
            var percentViewed = duration > 0 ? Math.Clamp(position / duration * 100, 0, 100) : 0;
            var itemType = item.GetType().Name;
            var completed = CustomNetflixPlaybackPolicy.IsCompleted(itemType, position, duration, percentViewed);
            eventType = completed ? "complete" : request.IsPaused ? "pause" : "progress";
            var lastPlayedAt = DateTime.UtcNow;
            var progress = new WatchProgressRow(
                profileId,
                request.ItemId,
                request.MediaSourceId,
                position,
                duration,
                percentViewed,
                completed,
                0,
                lastPlayedAt);
            var watchEvent = new WatchEventRow(
                Guid.NewGuid(),
                profileId,
                jellyfinUserId,
                request.ItemId,
                itemType,
                eventType,
                position,
                duration,
                request.PlaySessionId,
                request.ClientName);

            var progressDto = CustomNetflixDtoMapper.MapProgress(progress);
            if (eventType is "pause" or "complete")
            {
                await _repository.UpsertProgressAsync(progress, cancellationToken).ConfigureAwait(false);
                await InvalidateHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
                await _nativePlaystateSyncService.SyncProgressAsync(profile, user, item, progress, eventType, cancellationToken).ConfigureAwait(false);

                var saved = await _repository.GetProgressAsync(profileId, request.ItemId, cancellationToken).ConfigureAwait(false);
                if (saved is not null)
                {
                    progressDto = CustomNetflixDtoMapper.MapProgress(saved);
                }
            }
            else
            {
                await _watchProgressBuffer.EnqueueAsync(progress, cancellationToken).ConfigureAwait(false);
            }

            await _watchEventBuffer.EnqueueAsync(watchEvent, cancellationToken).ConfigureAwait(false);

            CustomNetflixMetrics.ObserveProgressReport(eventType is "progress" ? "queued" : "saved", eventType);
            return progressDto;
        }
        catch
        {
            CustomNetflixMetrics.ObserveProgressReport("error", eventType);
            throw;
        }
    }

    public async Task<IReadOnlyList<CustomNetflixContinueWatchingItemDto>> GetContinueWatchingAsync(Guid jellyfinUserId, Guid profileId, int limit, CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (profile is null || user is null)
        {
            return Array.Empty<CustomNetflixContinueWatchingItemDto>();
        }

        var rows = await _repository.GetContinueWatchingAsync(profileId, limit, cancellationToken).ConfigureAwait(false);
        var visibleItems = new List<(WatchProgressRow Row, BaseItem Item)>(rows.Count);
        foreach (var row in rows)
        {
            var item = _libraryManager.GetItemById<BaseItem>(row.ItemId, user);
            if (item is null)
            {
                continue;
            }

            visibleItems.Add((row, item));
        }

        var itemDtos = _cardDtoCache.GetBaseItemDtos(
            visibleItems.Select(entry => entry.Item).ToArray(),
            user);
        var items = new CustomNetflixContinueWatchingItemDto[visibleItems.Count];
        for (var index = 0; index < visibleItems.Count; index++)
        {
            items[index] = new CustomNetflixContinueWatchingItemDto
            {
                Item = itemDtos[index],
                Progress = CustomNetflixDtoMapper.MapProgress(visibleItems[index].Row)
            };
        }

        return items;
    }

    public async Task<IReadOnlyList<CustomNetflixWatchProgressDto>> GetProgressForItemsAsync(Guid jellyfinUserId, Guid profileId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken)
    {
        var normalizedItemIds = CustomNetflixWatchProgressBatchPolicy.NormalizeItemIds(itemIds);
        if (normalizedItemIds.Count == 0)
        {
            return Array.Empty<CustomNetflixWatchProgressDto>();
        }

        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
        {
            return Array.Empty<CustomNetflixWatchProgressDto>();
        }

        var rows = await _repository.GetProgressForItemsAsync(profileId, normalizedItemIds, cancellationToken).ConfigureAwait(false);
        return rows
            .Select(CustomNetflixDtoMapper.MapProgress)
            .ToArray();
    }

    public async Task<CustomNetflixWatchProgressDto?> SetPlayedAsync(Guid jellyfinUserId, Guid profileId, Guid itemId, bool played, CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (profile is null || user is null)
        {
            return null;
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return null;
        }

        var row = await _repository.SetPlayedAsync(profileId, jellyfinUserId, itemId, played, item.GetType().Name, cancellationToken).ConfigureAwait(false);
        await InvalidateHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        await _nativePlaystateSyncService.SyncPlayedAsync(profile, user, item, played, cancellationToken).ConfigureAwait(false);
        return CustomNetflixDtoMapper.MapProgress(row);
    }

    public async Task<bool> HideFromContinueWatchingAsync(Guid jellyfinUserId, Guid profileId, Guid itemId, CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
        {
            return false;
        }

        var hidden = await _repository.HideFromContinueWatchingAsync(profileId, itemId, cancellationToken).ConfigureAwait(false);
        await InvalidateHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        return hidden;
    }

    private async Task InvalidateHomeSnapshotsAsync(Guid profileId, CancellationToken cancellationToken)
    {
        await _repository.DeleteHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        await _cache.RemoveAsync(CustomNetflixHomeSnapshots.CacheKeys(profileId), cancellationToken).ConfigureAwait(false);
    }
}
