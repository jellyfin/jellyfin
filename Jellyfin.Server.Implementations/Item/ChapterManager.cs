using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// The Chapter manager.
/// </summary>
public class ChapterManager : IChapterManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IImageProcessor _imageProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChapterManager"/> class.
    /// </summary>
    /// <param name="dbProvider">The EFCore provider.</param>
    /// <param name="imageProcessor">The Image Processor.</param>
    public ChapterManager(IDbContextFactory<JellyfinDbContext> dbProvider, IImageProcessor imageProcessor)
    {
        _dbProvider = dbProvider;
        _imageProcessor = imageProcessor;
    }

    /// <inheritdoc cref="IChapterManager"/>
    public ChapterInfo? GetChapter(BaseItemDto baseItem, int index)
    {
        using var context = _dbProvider.CreateDbContext();
        var chapter = context.Chapters.FirstOrDefault(e => e.ItemId.Equals(baseItem.Id) && e.ChapterIndex == index);
        if (chapter is not null)
        {
            return Map(chapter, baseItem);
        }

        return null;
    }

    /// <inheritdoc cref="IChapterManager"/>
    public IReadOnlyList<ChapterInfo> GetChapters(BaseItemDto baseItem)
    {
        using var context = _dbProvider.CreateDbContext();
        return context.Chapters.Where(e => e.ItemId.Equals(baseItem.Id))
            .ToList()
            .Select(e => Map(e, baseItem))
            .ToImmutableArray();
    }

    /// <inheritdoc cref="IChapterManager"/>
    public void SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters)
    {
        using var context = _dbProvider.CreateDbContext();
        using (var transaction = context.Database.BeginTransaction())
        {
            context.Chapters.Where(e => e.ItemId.Equals(itemId)).ExecuteDelete();
            for (var i = 0; i < chapters.Count; i++)
            {
                var chapter = chapters[i];
                context.Chapters.Add(Map(chapter, i, itemId));
            }

            context.SaveChanges();
            transaction.Commit();
        }
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
