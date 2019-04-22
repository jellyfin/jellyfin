using System;
using System.Collections.Generic;
using Jellyfin.Controller.Chapters;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Persistence;
using Jellyfin.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Providers.Chapters
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
