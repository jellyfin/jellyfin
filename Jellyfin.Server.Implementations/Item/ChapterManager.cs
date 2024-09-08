using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;

namespace Jellyfin.Server.Implementations.Item;

public class ChapterManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    public ChapterManager(IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _dbProvider = dbProvider;
    }

    public IReadOnlyList<ChapterInfo> GetChapters(BaseItemDto baseItemDto)
    {
        using var context = _dbProvider.CreateDbContext();
        return context.Chapters.Where(e => e.ItemId.Equals(baseItemDto.Id)).Select(Map).ToList();
    }

    private Chapter Map(ChapterInfo chapterInfo, int index, Guid itemId)
    {
        return new Chapter()
        {
            ChapterIndex = index,
            StartPositionTicks = chapterInfo.StartPositionTicks,
            ImageDateModified = chapterInfo.ImageDateModified,
            ImagePath = chapterInfo.ImagePath,
            ItemId = itemId,
            Name = chapterInfo.Name
        };
    }

    private ChapterInfo Map(Chapter chapterInfo, BaseItemDto baseItem)
    {
        var info = new ChapterInfo()
        {
            StartPositionTicks = chapterInfo.StartPositionTicks,
            ImageDateModified = chapterInfo.ImageDateModified.GetValueOrDefault(),
            ImagePath = chapterInfo.ImagePath,
            Name = chapterInfo.Name,
        };
        info.ImageTag = _imageProcessor.GetImageCacheTag(baseItem, info);
        return info;
    }
}
