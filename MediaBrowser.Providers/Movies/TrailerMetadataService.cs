#pragma warning disable CS1591

using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Movies
{
    public class TrailerMetadataService : MetadataService<Trailer, TrailerInfo>
    {
        public TrailerMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<TrailerMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Trailer> source, MetadataResult<Trailer> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            if (replaceData || target.Item.TrailerTypes.Length == 0)
            {
                target.Item.TrailerTypes = source.Item.TrailerTypes;
            }
            else
            {
                target.Item.TrailerTypes = target.Item.TrailerTypes.Concat(source.Item.TrailerTypes).Distinct().ToArray();
            }
        }
    }
}
