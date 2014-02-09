using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class SeriesMetadataService : MetadataService<Series, SeriesInfo>
    {
        private readonly ILibraryManager _libraryManager;

        public SeriesMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Merges the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="lockedFields">The locked fields.</param>
        /// <param name="replaceData">if set to <c>true</c> [replace data].</param>
        /// <param name="mergeMetadataSettings">if set to <c>true</c> [merge metadata settings].</param>
        protected override void MergeData(Series source, Series target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        protected override ItemUpdateType BeforeSave(Series item)
        {
            var updateType = base.BeforeSave(item);

            var episodes = item.RecursiveChildren
                .OfType<Episode>()
                .ToList();

            var dateLastEpisodeAdded = item.DateLastEpisodeAdded;

            item.DateLastEpisodeAdded = episodes
                .Where(i => i.LocationType != LocationType.Virtual)
                .Select(i => i.DateCreated)
                .OrderByDescending(i => i)
                .FirstOrDefault();

            if (dateLastEpisodeAdded != item.DateLastEpisodeAdded)
            {
                updateType = updateType | ItemUpdateType.MetadataImport;
            }

            return updateType;
        }
    }
}
