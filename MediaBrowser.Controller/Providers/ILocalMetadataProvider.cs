using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface ILocalMetadataProvider : IMetadataProvider
    {
    }

    public interface ILocalMetadataProvider<TItemType> : IMetadataProvider<TItemType>, ILocalMetadataProvider
         where TItemType : IHasMetadata
    {
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MetadataResult{`0}}.</returns>
        Task<MetadataResult<TItemType>> GetMetadata(ItemInfo info, 
            IDirectoryService directoryService,
            CancellationToken cancellationToken);
    }
}
