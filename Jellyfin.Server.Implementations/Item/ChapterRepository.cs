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
public class ChapterRepository : IChapterRepository
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IImageProcessor _imageProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChapterRepository"/> class.
    /// </summary>
    /// <param name="dbProvider">The EFCore provider.</param>
    /// <param name="imageProcessor">The Image Processor.</param>
    public ChapterRepository(IDbContextFactory<JellyfinDbContext> dbProvider, IImageProcessor imageProcessor)
    {
        _dbProvider = dbProvider;
        _imageProcessor = imageProcessor;
    }

    /// <inheritdoc cref="IChapterRepository"/>
    public ChapterInfo? GetChapter(BaseItemDto baseItem, int index)
    {
        return GetChapter(baseItem.Id, index);
    }

    /// <inheritdoc cref="IChapterRepository"/>
    public IReadOnlyList<ChapterInfo> GetChapters(BaseItemDto baseItem)
    {
        return GetChapters(baseItem.Id);
    }

    /// <inheritdoc cref="IChapterRepository"/>
    public ChapterInfo? GetChapter(Guid baseItemId, int index)
    {
        using var context = _dbProvider.CreateDbContext();
        var chapter = context.Chapters.AsNoTracking()
            .Select(e => new
            {
                chapter = e,
                baseItemPath = e.Item.Path
            })
            .FirstOrDefault(e => e.chapter.ItemId.Equals(baseItemId) && e.chapter.ChapterIndex == index);
        if (chapter is not null)
        {
            return Map(chapter.chapter, chapter.baseItemPath!);
        }

        return null;
    }

    /// <inheritdoc cref="IChapterRepository"/>
    public IReadOnlyList<ChapterInfo> GetChapters(Guid baseItemId)
    {
        using var context = _dbProvider.CreateDbContext();
        return context.Chapters.AsNoTracking().Where(e => e.ItemId.Equals(baseItemId))
            .Select(e => new
            {
                chapter = e,
                baseItemPath = e.Item.Path
            })
            .AsEnumerable()
            .Select(e => Map(e.chapter, e.baseItemPath!))
            .ToArray();
    }

    /// <inheritdoc cref="IChapterRepository"/>
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
            Name = chapterInfo.Name,
            Item = null!
        };
    }

    private ChapterInfo Map(Chapter chapterInfo, string baseItemPath)
    {
        var chapterEntity = new ChapterInfo()
        {
            StartPositionTicks = chapterInfo.StartPositionTicks,
            ImageDateModified = chapterInfo.ImageDateModified.GetValueOrDefault(),
            ImagePath = chapterInfo.ImagePath,
            Name = chapterInfo.Name,
        };
        chapterEntity.ImageTag = _imageProcessor.GetImageCacheTag(baseItemPath, chapterEntity.ImageDateModified);
        return chapterEntity;
    }
}
