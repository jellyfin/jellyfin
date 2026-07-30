#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixWatchHistoryService : ICustomNetflixWatchHistoryService
{
    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixRepository _repository;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;

    public CustomNetflixWatchHistoryService(
        ICustomNetflixProfileService profileService,
        ICustomNetflixRepository repository,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService)
    {
        _profileService = profileService;
        _repository = repository;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
    }

    public async Task<IReadOnlyList<CustomNetflixWatchHistoryItemDto>> GetHistoryAsync(
        Guid jellyfinUserId,
        Guid profileId,
        int limit,
        CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (profile is null || user is null)
        {
            return Array.Empty<CustomNetflixWatchHistoryItemDto>();
        }

        var rows = await _repository.GetWatchHistoryAsync(profileId, CustomNetflixHistoryPolicy.NormalizeLimit(limit), cancellationToken).ConfigureAwait(false);
        var visibleItems = new List<(WatchHistoryRow Row, BaseItem Item)>(rows.Count);
        foreach (var row in rows)
        {
            var item = _libraryManager.GetItemById<BaseItem>(row.ItemId, user);
            if (item is null)
            {
                continue;
            }

            visibleItems.Add((row, item));
        }

        var itemDtos = _dtoService.GetBaseItemDtos(
            visibleItems.Select(entry => entry.Item).ToArray(),
            CustomNetflixDtoMapper.CreateCardOptions(),
            user,
            skipVisibilityCheck: true);
        var items = new CustomNetflixWatchHistoryItemDto[visibleItems.Count];
        for (var index = 0; index < visibleItems.Count; index++)
        {
            items[index] = new CustomNetflixWatchHistoryItemDto
            {
                Item = itemDtos[index],
                History = CustomNetflixDtoMapper.MapHistory(visibleItems[index].Row)
            };
        }

        return items;
    }
}
