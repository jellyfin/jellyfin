using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Folders
{
    public class FolderMetadataService : MetadataService<Folder, ItemLookupInfo>
    {
        public FolderMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<FolderMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        // Make sure the type-specific services get picked first
        public override int Order => 10;

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Folder> source, MetadataResult<Folder> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }
    }
}
