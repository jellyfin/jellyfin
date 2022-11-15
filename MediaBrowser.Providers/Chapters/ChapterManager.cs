#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Chapters
{
    public class ChapterManager : IChapterManager
    {
        private readonly IItemRepository _itemRepo;

        public ChapterManager(IItemRepository itemRepo)
        {
            _itemRepo = itemRepo;
        }

        /// <inheritdoc />
        public void SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters)
        {
            _itemRepo.SaveChapters(itemId, chapters);
        }
    }
}
