#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixCardDtoCache
{
    private readonly IDtoService _dtoService;
    private readonly Dictionary<(Guid UserId, Guid ItemId), BaseItemDto> _items = new();

    public CustomNetflixCardDtoCache(IDtoService dtoService)
    {
        _dtoService = dtoService;
    }

    public IReadOnlyList<BaseItemDto> GetBaseItemDtos(IReadOnlyList<BaseItem> items, User user)
    {
        lock (_items)
        {
            var missing = items
                .Where(item => !_items.ContainsKey((user.Id, item.Id)))
                .DistinctBy(item => item.Id)
                .ToArray();
            if (missing.Length > 0)
            {
                var hydrated = _dtoService.GetBaseItemDtos(
                    missing,
                    CustomNetflixDtoMapper.CreateCardOptions(),
                    user,
                    skipVisibilityCheck: true);
                if (hydrated.Count != missing.Length)
                {
                    throw new InvalidOperationException("Card DTO hydration returned an unexpected item count.");
                }

                for (var index = 0; index < missing.Length; index++)
                {
                    _items[(user.Id, missing[index].Id)] = hydrated[index];
                }
            }

            return items
                .Select(item => _items[(user.Id, item.Id)])
                .ToArray();
        }
    }
}
