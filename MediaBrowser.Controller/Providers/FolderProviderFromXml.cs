using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides metadata for Folders and all subclasses by parsing folder.xml
    /// </summary>
    [Export(typeof(BaseMetadataProvider))]
    public class FolderProviderFromXml : BaseMetadataProvider
    {
        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Folder && item.LocationType == LocationType.FileSystem;
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
            var entry = item.MetaLocation != null ? item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, "folder.xml")) : null;
            return entry != null ? entry.Value.LastWriteTimeUtc : DateTime.MinValue;
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
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool Fetch(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadataFile = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, "folder.xml"));

            if (metadataFile.HasValue)
            {
                var path = metadataFile.Value.Path;
                new BaseItemXmlParser<Folder>(Logger).Fetch((Folder)item, path, cancellationToken);
                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }

            return false;
        }
    }
}
