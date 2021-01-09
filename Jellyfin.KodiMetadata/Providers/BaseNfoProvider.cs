using System.Threading;
using System.Threading.Tasks;
using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.KodiMetadata.Providers
{
    /// <summary>
    /// The base nfo metadata provider.
    /// </summary>
    /// <typeparam name="T1">The media object for which to provide metadata.</typeparam>
    /// <typeparam name="T2">The nfo object type.</typeparam>
    public abstract class BaseNfoProvider<T1, T2> : ILocalMetadataProvider<T1>, IHasItemChangeMonitor
        where T1 : BaseItem, new()
        where T2 : BaseNfo, new()
    {
        /// <inheritdoc/>
        public string Name => "Nfo";

        /// <inheritdoc/>
        public Task<MetadataResult<T1>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            /*
             * Get Nfo File
             * Deserialize
             * Map Objects
             */
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Maps the <see cref="BaseNfo"/> object to the jellyfin <see cref="T2"/> object.
        /// </summary>
        /// <param name="nfo">The nfo object.</param>
        public virtual void MapNfoToJellyfinObject(T2 nfo)
        {
            /*
             * Map Objects
             */
            throw new System.NotImplementedException();
        }
    }
}
