using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class EpisodeProviderFromXml
    /// </summary>
    public class EpisodeProviderFromXml : BaseMetadataProvider
    {
        internal static EpisodeProviderFromXml Current { get; private set; }

        public EpisodeProviderFromXml(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
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
            get { return MetadataProviderPriority.Second; }
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
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            var metadataFile = Path.Combine(item.MetaLocation, Path.ChangeExtension(Path.GetFileName(item.Path), ".xml"));

            var file = item.ResolveArgs.Parent.ResolveArgs.GetMetaFileByPath(metadataFile);

            if (file == null)
            {
                return base.CompareDate(item);
            }

            return file.LastWriteTimeUtc;
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
                new EpisodeXmlParser(Logger).Fetch((Episode)item, metadataFile, cancellationToken);
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
