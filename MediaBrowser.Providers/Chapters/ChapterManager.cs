using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Providers.Chapters
{
    public class ChapterManager : IChapterManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IItemRepository _itemRepo;

        public ChapterManager(ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config, IItemRepository itemRepo)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _config = config;
            _itemRepo = itemRepo;
        }

        public void SaveChapters(string itemId, List<ChapterInfo> chapters)
        {
            _itemRepo.SaveChapters(new Guid(itemId), chapters);
        }
    }
}
