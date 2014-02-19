using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;

namespace MediaBrowser.Providers.Genres
{
    public class GenreMetadataService : MetadataService<Genre, ItemLookupInfo>
    {
        public GenreMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem)
            : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem)
        {
        }

        /// <summary>
        /// Merges the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="lockedFields">The locked fields.</param>
        /// <param name="replaceData">if set to <c>true</c> [replace data].</param>
        /// <param name="mergeMetadataSettings">if set to <c>true</c> [merge metadata settings].</param>
        protected override void MergeData(Genre source, Genre target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }
    }
}
