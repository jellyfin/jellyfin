using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Movie Metadata Service.
    /// </summary>
    public class MovieMetadataService : MetadataService<Movie, MovieInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovieMetadataService"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">The <see cref="IServerConfigurationManager"/> to use.</param>
        /// <param name="logger">A logger the service can use to log messages.</param>
        /// <param name="providerManager">The <see cref="IProviderManager"/> to use.</param>
        /// <param name="fileSystem">The <see cref="IFileSystem"/> to use.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> to use.</param>
        public MovieMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<MovieMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Movie> source, MetadataResult<Movie> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || string.IsNullOrEmpty(targetItem.CollectionName))
            {
                targetItem.CollectionName = sourceItem.CollectionName;
            }
        }
    }
}
