using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public IEnumerable<ChapterInfo> GetChapters(string itemId)
        {
            return _itemRepo.GetChapters(new Guid(itemId));
        }

        public Task SaveChapters(string itemId, List<ChapterInfo> chapters, CancellationToken cancellationToken)
        {
            return _itemRepo.SaveChapters(new Guid(itemId), chapters, cancellationToken);
        }

        public ChapterOptions GetConfiguration()
        {
            return _config.GetConfiguration<ChapterOptions>("chapters");
        }
    }

    public class ChapterConfigurationStore : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                    Key = "chapters",
                    ConfigurationType = typeof (ChapterOptions)
                }
            };
        }
    }
}
