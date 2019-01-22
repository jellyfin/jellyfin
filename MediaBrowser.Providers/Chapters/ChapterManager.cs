using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Chapters
{
    public class ChapterManager : IChapterManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IItemRepository _itemRepo;

        public ChapterManager(
            ILibraryManager libraryManager,
            ILoggerFactory loggerFactory,
            IServerConfigurationManager config,
            IItemRepository itemRepo)
        {
            _libraryManager = libraryManager;
            _logger = loggerFactory.CreateLogger(nameof(ChapterManager));
            _config = config;
            _itemRepo = itemRepo;
        }

        public void SaveChapters(string itemId, List<ChapterInfo> chapters)
        {
            _itemRepo.SaveChapters(new Guid(itemId), chapters);
        }
    }
}
