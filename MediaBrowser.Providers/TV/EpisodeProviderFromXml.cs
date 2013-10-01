using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class EpisodeProviderFromXml
    /// </summary>
    public class EpisodeProviderFromXml : BaseMetadataProvider
    {
        internal static EpisodeProviderFromXml Current { get; private set; }
        private readonly IItemRepository _itemRepo;

        public EpisodeProviderFromXml(ILogManager logManager, IServerConfigurationManager configurationManager, IItemRepository itemRepo)
            : base(logManager, configurationManager)
        {
            _itemRepo = itemRepo;
            Current = this;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Episode && item.LocationType == LocationType.FileSystem;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            return Fetch(item, cancellationToken);
        }

        /// <summary>
        /// Needses the refresh based on compare date.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var metadataFile = Path.Combine(item.MetaLocation, Path.ChangeExtension(Path.GetFileName(item.Path), ".xml"));

            var file = item.ResolveArgs.Parent.ResolveArgs.GetMetaFileByPath(metadataFile);

            if (file == null)
            {
                return false;
            }

            return FileSystem.GetLastWriteTimeUtc(file, Logger) > providerInfo.LastRefreshed;
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private async Task<bool> Fetch(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadataFile = Path.Combine(item.MetaLocation, Path.ChangeExtension(Path.GetFileName(item.Path), ".xml"));

            var file = item.ResolveArgs.Parent.ResolveArgs.GetMetaFileByPath(metadataFile);

            if (file == null)
            {
                return false;
            }

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await new EpisodeXmlParser(Logger, _itemRepo).FetchAsync((Episode)item, metadataFile, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                XmlParsingResourcePool.Release();
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }
    }
}
