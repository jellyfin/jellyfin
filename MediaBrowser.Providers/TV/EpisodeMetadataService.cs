using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class EpisodeMetadataService : MetadataService<Episode, EpisodeInfo>
    {
        private readonly ILibraryManager _libraryManager;

        public EpisodeMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, ILibraryManager libraryManager)
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
        protected override void MergeData(Episode source, Episode target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            if (replaceData || !target.AirsBeforeSeasonNumber.HasValue)
            {
                target.AirsBeforeSeasonNumber = source.AirsBeforeSeasonNumber;
            }

            if (replaceData || !target.AirsAfterSeasonNumber.HasValue)
            {
                target.AirsAfterSeasonNumber = source.AirsAfterSeasonNumber;
            }

            if (replaceData || !target.AirsBeforeEpisodeNumber.HasValue)
            {
                target.AirsBeforeEpisodeNumber = source.AirsBeforeEpisodeNumber;
            }

            if (replaceData || !target.DvdSeasonNumber.HasValue)
            {
                target.DvdSeasonNumber = source.DvdSeasonNumber;
            }

            if (replaceData || !target.DvdEpisodeNumber.HasValue)
            {
                target.DvdEpisodeNumber = source.DvdEpisodeNumber;
            }

            if (replaceData || !target.AbsoluteEpisodeNumber.HasValue)
            {
                target.AbsoluteEpisodeNumber = source.AbsoluteEpisodeNumber;
            }

            if (replaceData || !target.IndexNumberEnd.HasValue)
            {
                target.IndexNumberEnd = source.IndexNumberEnd;
            }
        }

        protected override ItemUpdateType BeforeMetadataRefresh(Episode item)
        {
            var updateType = base.BeforeMetadataRefresh(item);

            var locationType = item.LocationType;
            if (locationType == LocationType.FileSystem || locationType == LocationType.Offline)
            {
                var currentIndexNumber = item.IndexNumber;
                var currentIndexNumberEnd = item.IndexNumberEnd;
                var currentParentIndexNumber = item.ParentIndexNumber;

                var filename = Path.GetFileName(item.Path);

                item.IndexNumber = item.IndexNumber ?? TVUtils.GetEpisodeNumberFromFile(filename, item.Parent is Season);
                item.IndexNumberEnd = item.IndexNumberEnd ?? TVUtils.GetEndingEpisodeNumberFromFile(filename);

                if (!item.ParentIndexNumber.HasValue)
                {
                    var season = item.Season;

                    if (season != null)
                    {
                        item.ParentIndexNumber = season.IndexNumber;
                    }
                }

                if ((currentIndexNumber ?? -1) != (item.IndexNumber ?? -1))
                {
                    updateType = updateType | ItemUpdateType.MetadataImport;
                }

                if ((currentIndexNumberEnd ?? -1) != (item.IndexNumberEnd ?? -1))
                {
                    updateType = updateType | ItemUpdateType.MetadataImport;
                }

                if ((currentParentIndexNumber ?? -1) != (item.ParentIndexNumber ?? -1))
                {
                    updateType = updateType | ItemUpdateType.MetadataImport;
                }
            }

            return updateType;
        }
    }
}
