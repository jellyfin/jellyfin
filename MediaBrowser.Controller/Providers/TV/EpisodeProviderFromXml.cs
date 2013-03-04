using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Controller.Providers.TV
{
    /// <summary>
    /// Class EpisodeProviderFromXml
    /// </summary>
    public class EpisodeProviderFromXml : BaseMetadataProvider
    {
        public EpisodeProviderFromXml(ILogManager logManager, IServerConfigurationManager configurationManager) : base(logManager, configurationManager)
        {
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

        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            return Task.Run(() => Fetch(item, cancellationToken));
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

            if (!file.HasValue)
            {
                return base.CompareDate(item);
            }

            return file.Value.LastWriteTimeUtc;
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool Fetch(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var metadataFile = Path.Combine(item.MetaLocation, Path.ChangeExtension(Path.GetFileName(item.Path), ".xml"));

            var episode = (Episode)item;

            if (!FetchMetadata(episode, item.ResolveArgs.Parent, metadataFile, cancellationToken))
            {
                // Don't set last refreshed if we didn't do anything
                return false;
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Fetches the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool FetchMetadata(Episode item, Folder parent, string metadataFile, CancellationToken cancellationToken)
        {
            var file = parent.ResolveArgs.GetMetaFileByPath(metadataFile);

            if (!file.HasValue)
            {
                return false;
            }

            new EpisodeXmlParser(Logger).Fetch(item, metadataFile, cancellationToken);
            return true;
        }
    }
}
