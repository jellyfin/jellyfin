using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using CommonIO;

namespace MediaBrowser.Providers.Movies
{
    public class MovieMetadataService : MetadataService<Movie, MovieInfo>
    {
        public MovieMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem, userDataManager, libraryManager)
        {
        }

        protected override bool IsFullLocalMetadata(Movie item)
        {
            if (string.IsNullOrWhiteSpace(item.Overview))
            {
                return false;
            }
            if (!item.ProductionYear.HasValue)
            {
                return false;
            }
            return base.IsFullLocalMetadata(item);
        }

        protected override void MergeData(MetadataResult<Movie> source, MetadataResult<Movie> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || string.IsNullOrEmpty(targetItem.TmdbCollectionName))
            {
                targetItem.TmdbCollectionName = sourceItem.TmdbCollectionName;
            }
        }
    }
}
