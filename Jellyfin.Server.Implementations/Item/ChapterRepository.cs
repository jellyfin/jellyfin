using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// The Chapter manager.
/// </summary>
public class ChapterRepository : IChapterRepository
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IEntityFrameworkDatabaseLockingBehavior _writeBehavior;
    private readonly IImageProcessor _imageProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChapterRepository"/> class.
    /// </summary>
    /// <param name="dbProvider">The EFCore provider.</param>
    /// <param name="writeBehavior">Instance of the <see cref="IEntityFrameworkDatabaseLockingBehavior"/> interface.</param>
    /// <param name="imageProcessor">The Image Processor.</param>
    public ChapterRepository(IDbContextFactory<JellyfinDbContext> dbProvider, IEntityFrameworkDatabaseLockingBehavior writeBehavior, IImageProcessor imageProcessor)
    {
        _dbProvider = dbProvider;
        _writeBehavior = writeBehavior;
        _imageProcessor = imageProcessor;
    }

    /// <inheritdoc />
    public ChapterInfo? GetChapter(Guid baseItemId, int index)
    {
        var chapter = DoGetChapter(baseItemId, index);
        if (chapter is not null)
        {
            return Map(chapter.Chapter, chapter.BaseItemPath!);
        }

        return null;
    }

    private ChapterWithPath? DoGetChapter(Guid baseItemId, int index)
    {
        using var context = _dbProvider.CreateDbContext();
        using var dbLock = _writeBehavior.AcquireReaderLock(context);
        return context.Chapters.AsNoTracking()
            .Select(e => (ChapterWithPath?)new ChapterWithPath(e, e.Item.Path))
            .FirstOrDefault(e => e!.Chapter.ItemId.Equals(baseItemId) && e.Chapter.ChapterIndex == index);
    }

    /// <inheritdoc />
    public IReadOnlyList<ChapterInfo> GetChapters(Guid baseItemId)
    {
        return DoGetChapters(baseItemId)
            .Select(e => Map(e.Chapter, e.BaseItemPath!))
            .ToArray();
    }

    private ChapterWithPath[] DoGetChapters(Guid baseItemId)
    {
        using var context = _dbProvider.CreateDbContext();
        using var dbLock = _writeBehavior.AcquireReaderLock(context);
        return context.Chapters.AsNoTracking()
            .Where(e => e.ItemId.Equals(baseItemId))
            .Select(e => new ChapterWithPath(e, e.Item.Path))
            .ToArray();
    }

    /// <inheritdoc />
    public void SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters)
    {
        using var context = _dbProvider.CreateDbContext();
        using var dbLock = _writeBehavior.AcquireWriterLock(context);
        using var transaction = context.Database.BeginTransaction();
        context.Chapters.Where(e => e.ItemId.Equals(itemId)).ExecuteDelete();
        for (var i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];
            context.Chapters.Add(Map(chapter, i, itemId));
        }

        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc />
    public void DeleteChapters(Guid itemId)
    {
        using var context = _dbProvider.CreateDbContext();
        using var dbLock = _writeBehavior.AcquireWriterLock(context);
        context.Chapters.Where(c => c.ItemId.Equals(itemId)).ExecuteDelete();
        context.SaveChanges();
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

        if (!string.IsNullOrEmpty(chapterInfo.ImagePath))
        {
            chapterEntity.ImageTag = _imageProcessor.GetImageCacheTag(baseItemPath, chapterEntity.ImageDateModified);
        }

        return chapterEntity;
    }

    private record ChapterWithPath(Chapter Chapter, string? BaseItemPath);
}
