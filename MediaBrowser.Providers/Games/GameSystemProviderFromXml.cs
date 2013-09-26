using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Games
{
    public class GameSystemProviderFromXml : BaseMetadataProvider
    {
        internal static GameSystemProviderFromXml Current { get; private set; }

        public GameSystemProviderFromXml(ILogManager logManager, IServerConfigurationManager configurationManager)
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
            return item is GameSystem && item.LocationType == LocationType.FileSystem;
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
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, "gamesystem.xml"));
            return entry != null ? entry.LastWriteTimeUtc : DateTime.MinValue;
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
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private async Task<bool> Fetch(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadataFile = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, "gamesystem.xml"));

            if (metadataFile != null)
            {
                var path = metadataFile.FullName;

                await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    new BaseItemXmlParser<GameSystem>(Logger).Fetch((GameSystem)item, path, cancellationToken);
                }
                finally
                {
                    XmlParsingResourcePool.Release();
                }

                SetLastRefreshed(item, DateTime.UtcNow);

                return true;
            }

            return false;
        }
    }
}
