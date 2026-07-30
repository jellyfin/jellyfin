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

internal sealed class CustomNetflixMyListService : ICustomNetflixMyListService
{
    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixRepository _repository;
    private readonly ICustomNetflixCacheService _cache;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly CustomNetflixCardDtoCache _cardDtoCache;

    public CustomNetflixMyListService(
        ICustomNetflixProfileService profileService,
        ICustomNetflixRepository repository,
        ICustomNetflixCacheService cache,
        IUserManager userManager,
        ILibraryManager libraryManager,
        CustomNetflixCardDtoCache cardDtoCache)
    {
        _profileService = profileService;
        _repository = repository;
        _cache = cache;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _cardDtoCache = cardDtoCache;
    }

    public async Task<CustomNetflixMyListResponseDto?> GetMyListAsync(
        Guid jellyfinUserId,
        Guid profileId,
        int limit,
        CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (profile is null || user is null)
        {
            return null;
        }

        var rows = await _repository.GetMyListAsync(profileId, CustomNetflixMyListPolicy.NormalizeLimit(limit), cancellationToken).ConfigureAwait(false);
        var progressRows = await _repository.GetProgressForItemsAsync(profileId, rows.Select(row => row.ItemId).ToArray(), cancellationToken).ConfigureAwait(false);
        var progressByItemId = progressRows.ToDictionary(row => row.ItemId);
        var visibleItems = new List<(MyListRow Row, BaseItem Item)>(rows.Count);
        foreach (var row in rows)
        {
            var item = _libraryManager.GetItemById<BaseItem>(row.ItemId, user);
            if (item is null || !CustomNetflixMyListPolicy.SupportsItemType(item.GetType().Name))
            {
                continue;
            }

            visibleItems.Add((row, item));
        }

        var itemDtos = _cardDtoCache.GetBaseItemDtos(
            visibleItems.Select(entry => entry.Item).ToArray(),
            user);
        var items = new CustomNetflixMyListItemDto[visibleItems.Count];
        for (var index = 0; index < visibleItems.Count; index++)
        {
            var row = visibleItems[index].Row;
            items[index] = new CustomNetflixMyListItemDto
            {
                Item = itemDtos[index],
                AddedAt = row.AddedAt,
                Progress = progressByItemId.TryGetValue(row.ItemId, out var progress) ? CustomNetflixDtoMapper.MapProgress(progress) : null
            };
        }

        return new CustomNetflixMyListResponseDto
        {
            ProfileId = profileId,
            Items = items
        };
    }

    public async Task<CustomNetflixMyListStatusDto?> GetStatusAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var item = await GetSupportedItemAsync(jellyfinUserId, profileId, itemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return null;
        }

        var row = await _repository.GetMyListItemAsync(profileId, itemId, cancellationToken).ConfigureAwait(false);
        return MapStatus(profileId, itemId, row);
    }

    public async Task<CustomNetflixMyListStatusDto?> AddAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var item = await GetSupportedItemAsync(jellyfinUserId, profileId, itemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return null;
        }

        var row = await _repository.AddToMyListAsync(profileId, itemId, cancellationToken).ConfigureAwait(false);
        await InvalidateHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        return MapStatus(profileId, itemId, row);
    }

    public async Task<CustomNetflixMyListStatusDto?> RemoveAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var item = await GetSupportedItemAsync(jellyfinUserId, profileId, itemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return null;
        }

        var removed = await _repository.RemoveFromMyListAsync(profileId, itemId, cancellationToken).ConfigureAwait(false);
        if (removed)
        {
            await InvalidateHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        }

        return MapStatus(profileId, itemId, null);
    }

    private async Task<BaseItem?> GetSupportedItemAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (profile is null || user is null)
        {
            return null;
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        return item is not null && CustomNetflixMyListPolicy.SupportsItemType(item.GetType().Name) ? item : null;
    }

    private static CustomNetflixMyListStatusDto MapStatus(Guid profileId, Guid itemId, MyListRow? row)
        => new()
        {
            ProfileId = profileId,
            ItemId = itemId,
            IsInMyList = row is not null,
            AddedAt = row?.AddedAt
        };

    private async Task InvalidateHomeSnapshotsAsync(Guid profileId, CancellationToken cancellationToken)
    {
        await _repository.DeleteHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        await _cache.RemoveAsync(CustomNetflixHomeSnapshots.CacheKeys(profileId), cancellationToken).ConfigureAwait(false);
    }
}
